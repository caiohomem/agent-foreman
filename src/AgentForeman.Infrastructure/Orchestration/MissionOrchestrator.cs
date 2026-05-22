using AgentForeman.Core.Coding;
using AgentForeman.Core.Configuration;
using AgentForeman.Core.Git;
using AgentForeman.Core.Orchestration;
using AgentForeman.Core.Planning;
using AgentForeman.Core.PullRequests;
using AgentForeman.Core.Safety;
using AgentForeman.Core.State;
using AgentForeman.Core.Testing;
using AgentForeman.Core.WorkItems;

namespace AgentForeman.Infrastructure.Orchestration;

public sealed class MissionOrchestrator : IMissionOrchestrator
{
    private readonly IWorkItemProvider _workItemProvider;
    private readonly IPlanningAgent _planningAgent;
    private readonly ICodingAgent _codingAgent;
    private readonly ITestRunner _testRunner;
    private readonly ISafetyChecker _safetyChecker;
    private readonly IPullRequestProvider _pullRequestProvider;
    private readonly IGitRepository _gitRepository;
    private readonly IMissionRepository _missionRepository;
    private readonly IMissionBranchPreparer _branchPreparer;

    public MissionOrchestrator(
        IWorkItemProvider workItemProvider,
        IPlanningAgent planningAgent,
        ICodingAgent codingAgent,
        ITestRunner testRunner,
        ISafetyChecker safetyChecker,
        IPullRequestProvider pullRequestProvider,
        IGitRepository gitRepository,
        IMissionRepository missionRepository,
        IMissionBranchPreparer branchPreparer)
    {
        _workItemProvider = workItemProvider;
        _planningAgent = planningAgent;
        _codingAgent = codingAgent;
        _testRunner = testRunner;
        _safetyChecker = safetyChecker;
        _pullRequestProvider = pullRequestProvider;
        _gitRepository = gitRepository;
        _missionRepository = missionRepository;
        _branchPreparer = branchPreparer;
    }

    public async Task<RunOnceResult> RunOnceAsync(RunOnceRequest request, Action<string>? onProgress, CancellationToken cancellationToken)
    {
        var config = request.Config;
        var repoPath = config.Project.RepoPath;

        var readyItems = await _workItemProvider.GetReadyItemsAsync(cancellationToken);
        if (readyItems.Count == 0)
        {
            return new RunOnceResult(
                Success: true,
                PullRequestUrl: null,
                WorkItemId: null,
                WorkItemTitle: null,
                FinalStatus: null,
                ErrorMessage: null,
                NoReadyWorkItems: true,
                QuotaDetected: false,
                RetryAfter: null);
        }

        var item = readyItems[0];
        try { await _workItemProvider.MarkAsWorkingAsync(item, cancellationToken); } catch { }

        var missionId = $"{item.Source.ToString().ToLowerInvariant()}-{item.ExternalId}";
        var outputDirectory = Path.Combine(repoPath, ".agent", "runs", $"issue-{SanitizePathSegment(item.ExternalId)}");
        var agentsContent = ReadAgentsFile(repoPath);

        var now = DateTimeOffset.UtcNow;
        var existingMission = _missionRepository.GetById(missionId);
        var mission = (existingMission ?? new Mission(
                missionId,
                item.ExternalId,
                item.Source.ToString(),
                item.Title,
                MissionStatus.New,
                Branch: null,
                PlanPath: null,
                PullRequestUrl: null,
                RetryAfter: null,
                LastError: null,
                CreatedAt: now,
                UpdatedAt: now))
            with
            {
                Title = item.Title,
                Status = MissionStatus.Planning,
                LastError = null,
                UpdatedAt = now,
            };
        _missionRepository.Save(mission);

        onProgress?.Invoke("[1/4] Planning...");
        var planRequest = new PlanningRequest(
            item.ExternalId,
            item.Title,
            item.Body,
            item.Labels,
            item.Repository,
            repoPath,
            outputDirectory,
            agentsContent,
            config);

        PlanningResult planResult;
        try
        {
            planResult = await _planningAgent.CreatePlanAsync(planRequest, cancellationToken);
        }
        catch (Exception ex)
        {
            mission = mission with { Status = MissionStatus.Failed, LastError = ex.Message, UpdatedAt = DateTimeOffset.UtcNow };
            _missionRepository.Save(mission);
            return FailureResult(item, MissionStatus.Failed, ex.Message);
        }

        if (!planResult.Success)
        {
            if (IsQuotaFailure(planResult.Stdout, planResult.Stderr, planResult.ErrorMessage, config))
            {
                var retryAfter = DateTimeOffset.UtcNow.AddHours(config.Quota.RetryAfterHours ?? 1);
                var reason = "Claude quota or rate limit detected while creating the plan.";
                mission = mission with
                {
                    Status = MissionStatus.PausedQuota,
                    PlanPath = planResult.PlanPath,
                    RetryAfter = retryAfter,
                    LastError = planResult.ErrorMessage,
                    UpdatedAt = DateTimeOffset.UtcNow,
                };
                _missionRepository.Save(mission);
                await TryMarkAsPausedAsync(item, reason, retryAfter, cancellationToken);
                return new RunOnceResult(false, null, item.ExternalId, item.Title, MissionStatus.PausedQuota, planResult.ErrorMessage, false, true, retryAfter);
            }

            mission = mission with
            {
                Status = MissionStatus.Failed,
                PlanPath = planResult.PlanPath,
                LastError = planResult.ErrorMessage,
                UpdatedAt = DateTimeOffset.UtcNow,
            };
            _missionRepository.Save(mission);
            return FailureResult(item, MissionStatus.Failed, planResult.ErrorMessage ?? "Planning failed.");
        }

        mission = mission with
        {
            Status = MissionStatus.PlanReady,
            PlanPath = planResult.PlanPath,
            LastError = null,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
        _missionRepository.Save(mission);

        var agentBranch = $"agent/issue-{item.ExternalId}";
        var branchPrepResult = await _branchPreparer.PrepareAsync(
            new BranchPreparationRequest(repoPath, config.Project.DefaultBranch, agentBranch),
            cancellationToken);
        if (!branchPrepResult.Success)
        {
            mission = mission with { Status = MissionStatus.Failed, LastError = branchPrepResult.ErrorMessage, UpdatedAt = DateTimeOffset.UtcNow };
            _missionRepository.Save(mission);
            return FailureResult(item, MissionStatus.Failed, branchPrepResult.ErrorMessage ?? "Branch preparation failed.");
        }

        onProgress?.Invoke("[2/4] Executing...");
        mission = mission with { Status = MissionStatus.Coding, LastError = null, UpdatedAt = DateTimeOffset.UtcNow };
        _missionRepository.Save(mission);

        var codingRequest = new CodingRequest(
            item.ExternalId,
            item.Title,
            item.Body,
            item.Labels,
            item.Repository,
            repoPath,
            planResult.PlanPath,
            planResult.Stdout,
            outputDirectory,
            agentsContent,
            PreviousLogs: null,
            CurrentDiff: null,
            config);

        CodingResult codingResult;
        try
        {
            codingResult = await _codingAgent.ExecuteAsync(codingRequest, cancellationToken);
        }
        catch (Exception ex)
        {
            mission = mission with { Status = MissionStatus.Failed, LastError = ex.Message, UpdatedAt = DateTimeOffset.UtcNow };
            _missionRepository.Save(mission);
            return FailureResult(item, MissionStatus.Failed, ex.Message);
        }

        if (!codingResult.Success)
        {
            if (codingResult.QuotaDetected || IsQuotaFailure(codingResult.Stdout, codingResult.Stderr, codingResult.ErrorMessage, config))
            {
                var retryAfter = DateTimeOffset.UtcNow.AddHours(config.Quota.RetryAfterHours ?? 1);
                mission = mission with
                {
                    Status = MissionStatus.PausedQuota,
                    RetryAfter = retryAfter,
                    LastError = codingResult.ErrorMessage,
                    UpdatedAt = DateTimeOffset.UtcNow,
                };
                _missionRepository.Save(mission);
                try { await _workItemProvider.AddCommentAsync(item, $"Codex quota or rate limit detected. Retry after: {retryAfter:O}", cancellationToken); } catch { }
                return new RunOnceResult(false, null, item.ExternalId, item.Title, MissionStatus.PausedQuota, codingResult.ErrorMessage, false, true, retryAfter);
            }

            mission = mission with { Status = MissionStatus.Failed, LastError = codingResult.ErrorMessage, UpdatedAt = DateTimeOffset.UtcNow };
            _missionRepository.Save(mission);
            return FailureResult(item, MissionStatus.Failed, codingResult.ErrorMessage ?? "Execution failed.");
        }

        mission = mission with { Status = MissionStatus.CodingCompleted, LastError = null, UpdatedAt = DateTimeOffset.UtcNow };
        _missionRepository.Save(mission);

        onProgress?.Invoke("[3/4] Verifying...");
        var safetyResult = await _safetyChecker.CheckAsync(repoPath, config.Safety, cancellationToken);
        if (!safetyResult.Passed)
        {
            var violationMessages = string.Join(", ", safetyResult.Violations.Select(v => v.Message));
            var errorMessage = $"Safety check failed: {violationMessages}";
            mission = mission with { Status = MissionStatus.Failed, LastError = errorMessage, UpdatedAt = DateTimeOffset.UtcNow };
            _missionRepository.Save(mission);
            return FailureResult(item, MissionStatus.Failed, errorMessage);
        }

        var testRequest = new TestRunRequest(item.ExternalId, repoPath, config.Tests.Commands, outputDirectory);
        TestRunResult testResult;
        try
        {
            testResult = await _testRunner.RunAsync(testRequest, cancellationToken);
        }
        catch (Exception ex)
        {
            mission = mission with { Status = MissionStatus.TestsFailed, LastError = ex.Message, UpdatedAt = DateTimeOffset.UtcNow };
            _missionRepository.Save(mission);
            return FailureResult(item, MissionStatus.TestsFailed, ex.Message);
        }

        if (!testResult.Success)
        {
            mission = mission with { Status = MissionStatus.TestsFailed, LastError = testResult.ErrorMessage, UpdatedAt = DateTimeOffset.UtcNow };
            _missionRepository.Save(mission);
            return FailureResult(item, MissionStatus.TestsFailed, testResult.ErrorMessage ?? "Tests failed.");
        }

        mission = mission with { Status = MissionStatus.TestsPassed, LastError = null, UpdatedAt = DateTimeOffset.UtcNow };
        _missionRepository.Save(mission);

        onProgress?.Invoke("[4/4] Submitting...");
        mission = mission with { Branch = agentBranch, UpdatedAt = DateTimeOffset.UtcNow };
        _missionRepository.Save(mission);

        await _gitRepository.AddAllAsync(repoPath, cancellationToken);

        var commitMessage = $"Implement issue #{item.ExternalId}: {item.Title}";
        var commitResult = await _gitRepository.CommitAsync(repoPath, commitMessage, cancellationToken);
        if (!commitResult.Created)
        {
            mission = mission with { Status = MissionStatus.Failed, LastError = "Nothing to commit.", UpdatedAt = DateTimeOffset.UtcNow };
            _missionRepository.Save(mission);
            return FailureResult(item, MissionStatus.Failed, "Nothing to commit.");
        }

        try
        {
            await _gitRepository.PushAsync(repoPath, "origin", agentBranch, cancellationToken);
        }
        catch (Exception ex)
        {
            mission = mission with { Status = MissionStatus.Failed, LastError = ex.Message, UpdatedAt = DateTimeOffset.UtcNow };
            _missionRepository.Save(mission);
            return FailureResult(item, MissionStatus.Failed, $"Push failed: {ex.Message}");
        }

        var prRequest = new PullRequestRequest(
            item.ExternalId,
            item.Title,
            item.Body,
            item.Repository,
            repoPath,
            agentBranch,
            config.Project.DefaultBranch,
            commitMessage,
            $"Implement issue #{item.ExternalId}: {item.Title}",
            BuildPrBody(item, outputDirectory));

        PullRequestResult prResult;
        try
        {
            prResult = await _pullRequestProvider.CreateAsync(prRequest, cancellationToken);
        }
        catch (Exception ex)
        {
            mission = mission with { Status = MissionStatus.Failed, LastError = ex.Message, UpdatedAt = DateTimeOffset.UtcNow };
            _missionRepository.Save(mission);
            return FailureResult(item, MissionStatus.Failed, $"PR creation failed: {ex.Message}");
        }

        if (!prResult.Success)
        {
            mission = mission with { Status = MissionStatus.Failed, LastError = prResult.ErrorMessage, UpdatedAt = DateTimeOffset.UtcNow };
            _missionRepository.Save(mission);
            return FailureResult(item, MissionStatus.Failed, $"PR creation failed: {prResult.ErrorMessage}");
        }

        mission = mission with
        {
            Status = MissionStatus.PullRequestCreated,
            PullRequestUrl = prResult.PullRequestUrl,
            LastError = null,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
        _missionRepository.Save(mission);

        await TryMarkAsReviewAsync(item, prResult.PullRequestUrl!, cancellationToken);

        return new RunOnceResult(
            Success: true,
            PullRequestUrl: prResult.PullRequestUrl,
            WorkItemId: item.ExternalId,
            WorkItemTitle: item.Title,
            FinalStatus: MissionStatus.PullRequestCreated,
            ErrorMessage: null,
            NoReadyWorkItems: false,
            QuotaDetected: false,
            RetryAfter: null);
    }

    public async Task<ResumeResult> ResumeAsync(ResumeRequest request, Action<string>? onProgress, CancellationToken cancellationToken)
    {
        var config = request.Config;
        var mission = request.Mission;
        var item = request.WorkItem;
        var repoPath = config.Project.RepoPath;
        var outputDirectory = Path.Combine(repoPath, ".agent", "runs", $"issue-{SanitizePathSegment(item.ExternalId)}");
        var planFilePath = Path.Combine(outputDirectory, "plan.md");
        var codexLogPath = Path.Combine(outputDirectory, "codex-exec.log");

        var agentBranch = $"agent/issue-{item.ExternalId}";
        var startStage = DetermineStartStage(mission, planFilePath);
        var stages = GetStages(startStage);
        var total = stages.Count;
        var idx = 1;

        onProgress?.Invoke($"Resuming mission {mission.Id}");

        string? planContent = null;
        var currentPlanPath = mission.PlanPath ?? planFilePath;

        if (stages.Contains(ResumeStage.Plan))
        {
            onProgress?.Invoke($"[{idx++}/{total}] Planning...");
            mission = mission with { Status = MissionStatus.Planning, LastError = null, UpdatedAt = DateTimeOffset.UtcNow };
            _missionRepository.Save(mission);

            var agentsContent = ReadAgentsFile(repoPath);
            var planRequest = new PlanningRequest(
                item.ExternalId, item.Title, item.Body, item.Labels,
                item.Repository, repoPath, outputDirectory, agentsContent, config);

            PlanningResult planResult;
            try
            {
                planResult = await _planningAgent.CreatePlanAsync(planRequest, cancellationToken);
            }
            catch (Exception ex)
            {
                mission = mission with { Status = MissionStatus.Failed, LastError = ex.Message, UpdatedAt = DateTimeOffset.UtcNow };
                _missionRepository.Save(mission);
                return ResumeFailureResult(MissionStatus.Failed, ex.Message);
            }

            if (!planResult.Success)
            {
                if (IsQuotaFailure(planResult.Stdout, planResult.Stderr, planResult.ErrorMessage, config))
                {
                    var retryAfter = DateTimeOffset.UtcNow.AddHours(config.Quota.RetryAfterHours ?? 1);
                    var reason = "Claude quota or rate limit detected while creating the plan.";
                    mission = mission with { Status = MissionStatus.PausedQuota, PlanPath = planResult.PlanPath, RetryAfter = retryAfter, LastError = planResult.ErrorMessage, UpdatedAt = DateTimeOffset.UtcNow };
                    _missionRepository.Save(mission);
                    await TryMarkAsPausedAsync(item, reason, retryAfter, cancellationToken);
                    return new ResumeResult(false, null, MissionStatus.PausedQuota, planResult.ErrorMessage, true, retryAfter);
                }

                mission = mission with { Status = MissionStatus.Failed, PlanPath = planResult.PlanPath, LastError = planResult.ErrorMessage, UpdatedAt = DateTimeOffset.UtcNow };
                _missionRepository.Save(mission);
                return ResumeFailureResult(MissionStatus.Failed, planResult.ErrorMessage ?? "Planning failed.");
            }

            mission = mission with { Status = MissionStatus.PlanReady, PlanPath = planResult.PlanPath, LastError = null, UpdatedAt = DateTimeOffset.UtcNow };
            _missionRepository.Save(mission);
            currentPlanPath = planResult.PlanPath;
            planContent = planResult.Stdout;
        }

        if (stages.Contains(ResumeStage.Execute))
        {
            onProgress?.Invoke($"[{idx++}/{total}] Executing...");

            var resumeBranchPrepResult = await _branchPreparer.PrepareAsync(
                new BranchPreparationRequest(repoPath, config.Project.DefaultBranch, agentBranch),
                cancellationToken);
            if (!resumeBranchPrepResult.Success)
            {
                mission = mission with { Status = MissionStatus.Failed, LastError = resumeBranchPrepResult.ErrorMessage, UpdatedAt = DateTimeOffset.UtcNow };
                _missionRepository.Save(mission);
                return ResumeFailureResult(MissionStatus.Failed, resumeBranchPrepResult.ErrorMessage ?? "Branch preparation failed.");
            }

            if (planContent is null)
                planContent = File.Exists(currentPlanPath) ? File.ReadAllText(currentPlanPath) : string.Empty;

            var previousLogs = File.Exists(codexLogPath) ? File.ReadAllText(codexLogPath) : null;
            var diffText = await _gitRepository.GetDiffAsync(repoPath, cancellationToken);
            var currentDiff = string.IsNullOrEmpty(diffText) ? null : diffText;

            mission = mission with { Status = MissionStatus.Coding, LastError = null, UpdatedAt = DateTimeOffset.UtcNow };
            _missionRepository.Save(mission);

            var agentsContent = ReadAgentsFile(repoPath);
            var codingRequest = new CodingRequest(
                item.ExternalId, item.Title, item.Body, item.Labels, item.Repository,
                repoPath, currentPlanPath, planContent,
                outputDirectory, agentsContent, previousLogs, currentDiff, config);

            CodingResult codingResult;
            try
            {
                codingResult = await _codingAgent.ExecuteAsync(codingRequest, cancellationToken);
            }
            catch (Exception ex)
            {
                mission = mission with { Status = MissionStatus.Failed, LastError = ex.Message, UpdatedAt = DateTimeOffset.UtcNow };
                _missionRepository.Save(mission);
                return ResumeFailureResult(MissionStatus.Failed, ex.Message);
            }

            if (!codingResult.Success)
            {
                if (codingResult.QuotaDetected || IsQuotaFailure(codingResult.Stdout, codingResult.Stderr, codingResult.ErrorMessage, config))
                {
                    var retryAfter = DateTimeOffset.UtcNow.AddHours(config.Quota.RetryAfterHours ?? 1);
                    mission = mission with { Status = MissionStatus.PausedQuota, RetryAfter = retryAfter, LastError = codingResult.ErrorMessage, UpdatedAt = DateTimeOffset.UtcNow };
                    _missionRepository.Save(mission);
                    try { await _workItemProvider.AddCommentAsync(item, $"Codex quota or rate limit detected. Retry after: {retryAfter:O}", cancellationToken); } catch { }
                    return new ResumeResult(false, null, MissionStatus.PausedQuota, codingResult.ErrorMessage, true, retryAfter);
                }

                mission = mission with { Status = MissionStatus.Failed, LastError = codingResult.ErrorMessage, UpdatedAt = DateTimeOffset.UtcNow };
                _missionRepository.Save(mission);
                return ResumeFailureResult(MissionStatus.Failed, codingResult.ErrorMessage ?? "Execution failed.");
            }

            mission = mission with { Status = MissionStatus.CodingCompleted, LastError = null, UpdatedAt = DateTimeOffset.UtcNow };
            _missionRepository.Save(mission);
        }

        if (stages.Contains(ResumeStage.Verify))
        {
            onProgress?.Invoke($"[{idx++}/{total}] Verifying...");
            mission = mission with { Status = MissionStatus.Testing, LastError = null, UpdatedAt = DateTimeOffset.UtcNow };
            _missionRepository.Save(mission);

            var safetyResult = await _safetyChecker.CheckAsync(repoPath, config.Safety, cancellationToken);
            if (!safetyResult.Passed)
            {
                var violationMessages = string.Join(", ", safetyResult.Violations.Select(v => v.Message));
                var errorMessage = $"Safety check failed: {violationMessages}";
                mission = mission with { Status = MissionStatus.Failed, LastError = errorMessage, UpdatedAt = DateTimeOffset.UtcNow };
                _missionRepository.Save(mission);
                return ResumeFailureResult(MissionStatus.Failed, errorMessage);
            }

            var testRequest = new TestRunRequest(item.ExternalId, repoPath, config.Tests.Commands, outputDirectory);
            TestRunResult testResult;
            try
            {
                testResult = await _testRunner.RunAsync(testRequest, cancellationToken);
            }
            catch (Exception ex)
            {
                mission = mission with { Status = MissionStatus.TestsFailed, LastError = ex.Message, UpdatedAt = DateTimeOffset.UtcNow };
                _missionRepository.Save(mission);
                return ResumeFailureResult(MissionStatus.TestsFailed, ex.Message);
            }

            if (!testResult.Success)
            {
                mission = mission with { Status = MissionStatus.TestsFailed, LastError = testResult.ErrorMessage, UpdatedAt = DateTimeOffset.UtcNow };
                _missionRepository.Save(mission);
                return ResumeFailureResult(MissionStatus.TestsFailed, testResult.ErrorMessage ?? "Tests failed.");
            }

            mission = mission with { Status = MissionStatus.TestsPassed, LastError = null, UpdatedAt = DateTimeOffset.UtcNow };
            _missionRepository.Save(mission);
        }

        if (stages.Contains(ResumeStage.Submit))
        {
            onProgress?.Invoke($"[{idx++}/{total}] Submitting...");
            mission = mission with { Branch = agentBranch, UpdatedAt = DateTimeOffset.UtcNow };
            _missionRepository.Save(mission);

            await _gitRepository.AddAllAsync(repoPath, cancellationToken);

            var commitMessage = $"Implement issue #{item.ExternalId}: {item.Title}";
            var commitResult = await _gitRepository.CommitAsync(repoPath, commitMessage, cancellationToken);
            if (!commitResult.Created)
            {
                mission = mission with { Status = MissionStatus.Failed, LastError = "Nothing to commit.", UpdatedAt = DateTimeOffset.UtcNow };
                _missionRepository.Save(mission);
                return ResumeFailureResult(MissionStatus.Failed, "Nothing to commit.");
            }

            try
            {
                await _gitRepository.PushAsync(repoPath, "origin", agentBranch, cancellationToken);
            }
            catch (Exception ex)
            {
                mission = mission with { Status = MissionStatus.Failed, LastError = ex.Message, UpdatedAt = DateTimeOffset.UtcNow };
                _missionRepository.Save(mission);
                return ResumeFailureResult(MissionStatus.Failed, $"Push failed: {ex.Message}");
            }

            var prRequest = new PullRequestRequest(
                item.ExternalId, item.Title, item.Body, item.Repository, repoPath,
                agentBranch, config.Project.DefaultBranch,
                commitMessage,
                $"Implement issue #{item.ExternalId}: {item.Title}",
                BuildPrBody(item, outputDirectory));

            PullRequestResult prResult;
            try
            {
                prResult = await _pullRequestProvider.CreateAsync(prRequest, cancellationToken);
            }
            catch (Exception ex)
            {
                mission = mission with { Status = MissionStatus.Failed, LastError = ex.Message, UpdatedAt = DateTimeOffset.UtcNow };
                _missionRepository.Save(mission);
                return ResumeFailureResult(MissionStatus.Failed, $"PR creation failed: {ex.Message}");
            }

            if (!prResult.Success)
            {
                mission = mission with { Status = MissionStatus.Failed, LastError = prResult.ErrorMessage, UpdatedAt = DateTimeOffset.UtcNow };
                _missionRepository.Save(mission);
                return ResumeFailureResult(MissionStatus.Failed, $"PR creation failed: {prResult.ErrorMessage}");
            }

            mission = mission with { Status = MissionStatus.PullRequestCreated, PullRequestUrl = prResult.PullRequestUrl, LastError = null, UpdatedAt = DateTimeOffset.UtcNow };
            _missionRepository.Save(mission);
            await TryMarkAsReviewAsync(item, prResult.PullRequestUrl!, cancellationToken);

            return new ResumeResult(true, prResult.PullRequestUrl, MissionStatus.PullRequestCreated, null, false, null);
        }

        return new ResumeResult(true, null, mission.Status, null, false, null);
    }

    public async Task<DaemonTickResult> DaemonTickAsync(DaemonTickRequest request, Action<string>? onProgress, CancellationToken cancellationToken)
    {
        var config = request.Config;

        foreach (var status in new[] { MissionStatus.Planning, MissionStatus.Coding, MissionStatus.Testing })
        {
            var active = _missionRepository.GetByStatus(status, 1);
            if (active.Count > 0)
            {
                return new DaemonTickResult(true, false, $"Active mission: {active[0].Id}", null);
            }
        }

        var pausedMissions = _missionRepository.GetByStatus(MissionStatus.PausedQuota, 10);
        var readyPaused = pausedMissions.FirstOrDefault(m => !m.RetryAfter.HasValue || m.RetryAfter <= DateTimeOffset.UtcNow);

        if (readyPaused is not null)
        {
            var externalId = readyPaused.ExternalWorkItemId ?? string.Empty;
            onProgress?.Invoke($"Resuming paused mission {readyPaused.Id}...");

            WorkItem item;
            try
            {
                item = await _workItemProvider.GetWorkItemAsync(externalId, cancellationToken);
            }
            catch (Exception ex)
            {
                return new DaemonTickResult(false, false, $"Failed to get work item for paused mission {readyPaused.Id}: {ex.Message}", null);
            }

            var resumeResult = await ResumeAsync(new ResumeRequest(config, readyPaused, item, false), onProgress, cancellationToken);
            return new DaemonTickResult(false, false, $"Resumed mission {readyPaused.Id}", resumeResult.PullRequestUrl);
        }

        var runOnceResult = await RunOnceAsync(new RunOnceRequest(config), onProgress, cancellationToken);
        if (runOnceResult.NoReadyWorkItems)
        {
            return new DaemonTickResult(false, true, null, null);
        }

        return new DaemonTickResult(false, false, null, runOnceResult.PullRequestUrl);
    }

    private async Task TryMarkAsReviewAsync(WorkItem item, string pullRequestUrl, CancellationToken cancellationToken)
    {
        try { await _workItemProvider.MarkAsReviewAsync(item, pullRequestUrl, cancellationToken); }
        catch { }
    }

    private async Task TryMarkAsPausedAsync(WorkItem item, string reason, DateTimeOffset retryAfter, CancellationToken cancellationToken)
    {
        try { await _workItemProvider.MarkAsPausedAsync(item, reason, retryAfter, cancellationToken); }
        catch { }
    }

    private static ResumeResult ResumeFailureResult(MissionStatus status, string errorMessage) =>
        new(false, null, status, errorMessage, false, null);

    private static ResumeStage DetermineStartStage(Mission mission, string planFilePath)
    {
        return mission.Status switch
        {
            MissionStatus.PlanReady => ResumeStage.Execute,
            MissionStatus.Coding => ResumeStage.Execute,
            MissionStatus.CodingCompleted => ResumeStage.Verify,
            MissionStatus.Testing => ResumeStage.Verify,
            MissionStatus.TestsFailed => ResumeStage.Verify,
            MissionStatus.TestsPassed => ResumeStage.Submit,
            _ => File.Exists(planFilePath) ? ResumeStage.Execute : ResumeStage.Plan,
        };
    }

    private static IReadOnlyList<ResumeStage> GetStages(ResumeStage startStage)
    {
        var all = new[] { ResumeStage.Plan, ResumeStage.Execute, ResumeStage.Verify, ResumeStage.Submit };
        return all.SkipWhile(s => s != startStage).ToArray();
    }

    private enum ResumeStage { Plan, Execute, Verify, Submit }

    private static RunOnceResult FailureResult(WorkItem item, MissionStatus status, string errorMessage)
    {
        return new RunOnceResult(false, null, item.ExternalId, item.Title, status, errorMessage, false, false, null);
    }

    private static bool IsQuotaFailure(string stdout, string stderr, string? errorMessage, AgentForemanConfig config)
    {
        var output = $"{stdout}{Environment.NewLine}{stderr}{Environment.NewLine}{errorMessage}";
        return config.Quota.QuotaPatterns.Any(pattern =>
            !string.IsNullOrWhiteSpace(pattern)
            && output.Contains(pattern, StringComparison.OrdinalIgnoreCase));
    }

    private static string? ReadAgentsFile(string repoPath)
    {
        var path = Path.Combine(repoPath, "AGENTS.md");
        return File.Exists(path) ? File.ReadAllText(path) : null;
    }

    private static string SanitizePathSegment(string value)
    {
        foreach (var invalid in Path.GetInvalidFileNameChars().Concat(new[] { '/', '\\' }))
        {
            value = value.Replace(invalid, '-');
        }

        return value;
    }

    private static string BuildPrBody(WorkItem item, string outputDirectory)
    {
        var planPath = Path.Combine(outputDirectory, "plan.md");
        var execLogPath = Path.Combine(outputDirectory, "codex-exec.log");
        var testsLogPath = Path.Combine(outputDirectory, "tests.log");

        var lines = new List<string>
        {
            $"## Issue #{item.ExternalId}: {item.Title}",
            string.Empty,
        };

        if (!string.IsNullOrWhiteSpace(item.Url))
        {
            lines.Add($"Issue: {item.Url}");
            lines.Add(string.Empty);
        }

        lines.Add("This pull request was generated by **Agent Foreman** and requires human review before merging.");
        lines.Add(string.Empty);
        lines.Add("### Artifacts");
        lines.Add($"- Plan: `{planPath}`");
        lines.Add($"- Codex log: `{execLogPath}`");

        if (File.Exists(testsLogPath))
        {
            lines.Add($"- Tests log: `{testsLogPath}`");
        }

        return string.Join(Environment.NewLine, lines);
    }
}
