using AgentForeman.Cli;
using AgentForeman.Core.Coding;
using AgentForeman.Core.Configuration;
using AgentForeman.Core.Git;
using AgentForeman.Core.Planning;
using AgentForeman.Core.Safety;
using AgentForeman.Core.State;
using AgentForeman.Core.Testing;
using AgentForeman.Core.WorkItems;

namespace AgentForeman.Tests;

public sealed class CliRunOnceTests
{
    [Fact]
    public void RunOnceReturnsZeroExitCodeWhenNoReadyItems()
    {
        var services = RunOnceTestServices.Valid(hasReadyItems: false);

        var result = services.Execute(new[] { "run-once" });

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("No ready work items", result.Output);
    }

    [Fact]
    public void RunOncePrintsProgressMessages()
    {
        var services = RunOnceTestServices.Valid();

        var result = services.Execute(new[] { "run-once" });

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("[1/4] Planning...", result.Output);
        Assert.Contains("[2/4] Executing...", result.Output);
        Assert.Contains("[3/4] Verifying...", result.Output);
        Assert.Contains("[4/4] Submitting...", result.Output);
    }

    [Fact]
    public void RunOnceMarksMissionAsPullRequestCreated()
    {
        var services = RunOnceTestServices.Valid();

        var result = services.Execute(new[] { "run-once" });

        Assert.Equal(0, result.ExitCode);
        Assert.Equal(MissionStatus.PullRequestCreated, services.Missions.Saved.Last().Status);
        Assert.Contains("Pull request created:", result.Output);
    }

    [Fact]
    public void RunOnceMarksMissionAsFailedWhenPlanningFails()
    {
        var services = RunOnceTestServices.Valid(
            planningResult: PlanningResult.Failure(
                PlanPath: "/workspace/project/.agent/runs/issue-42/plan.md",
                LogPath: "/workspace/project/.agent/runs/issue-42/claude-plan.log",
                Stdout: string.Empty,
                Stderr: "planner crashed",
                ExitCode: 1,
                StartedAt: DateTimeOffset.UtcNow,
                FinishedAt: DateTimeOffset.UtcNow,
                ErrorMessage: "planner crashed"));

        var result = services.Execute(new[] { "run-once" });

        Assert.NotEqual(0, result.ExitCode);
        Assert.Equal(MissionStatus.Failed, services.Missions.Saved.Last().Status);
    }

    [Fact]
    public void RunOnceMarksMissionAsPausedQuotaWhenPlanningHitsQuota()
    {
        var services = RunOnceTestServices.Valid(
            planningResult: PlanningResult.Failure(
                PlanPath: "/workspace/project/.agent/runs/issue-42/plan.md",
                LogPath: "/workspace/project/.agent/runs/issue-42/claude-plan.log",
                Stdout: "usage limit reached",
                Stderr: string.Empty,
                ExitCode: 1,
                StartedAt: DateTimeOffset.UtcNow,
                FinishedAt: DateTimeOffset.UtcNow,
                ErrorMessage: "usage limit reached"));

        var result = services.Execute(new[] { "run-once" });

        Assert.NotEqual(0, result.ExitCode);
        Assert.Equal(MissionStatus.PausedQuota, services.Missions.Saved.Last().Status);
        Assert.NotNull(services.Missions.Saved.Last().RetryAfter);
        Assert.Contains("quota", services.WorkItems.PausedReason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RunOnceMarksMissionAsFailedWhenCodingFails()
    {
        var services = RunOnceTestServices.Valid(
            codingResult: CodingResult.Failure(
                logPath: "/workspace/project/.agent/runs/issue-42/codex-exec.log",
                stdout: string.Empty,
                stderr: "codex crashed",
                exitCode: 1,
                startedAt: DateTimeOffset.UtcNow,
                finishedAt: DateTimeOffset.UtcNow,
                errorMessage: "codex crashed"));

        var result = services.Execute(new[] { "run-once" });

        Assert.NotEqual(0, result.ExitCode);
        Assert.Equal(MissionStatus.Failed, services.Missions.Saved.Last().Status);
    }

    [Fact]
    public void RunOnceMarksMissionAsPausedQuotaWhenCodingHitsQuota()
    {
        var services = RunOnceTestServices.Valid(
            codingResult: CodingResult.Failure(
                logPath: "/workspace/project/.agent/runs/issue-42/codex-exec.log",
                stdout: string.Empty,
                stderr: string.Empty,
                exitCode: 1,
                startedAt: DateTimeOffset.UtcNow,
                finishedAt: DateTimeOffset.UtcNow,
                errorMessage: "quota exceeded",
                quotaDetected: true));

        var result = services.Execute(new[] { "run-once" });

        Assert.NotEqual(0, result.ExitCode);
        Assert.Equal(MissionStatus.PausedQuota, services.Missions.Saved.Last().Status);
        Assert.NotNull(services.Missions.Saved.Last().RetryAfter);
        Assert.Contains("quota", services.WorkItems.LastComment, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RunOnceMarksMissionAsTestsFailedWhenVerifyFails()
    {
        var services = RunOnceTestServices.Valid(
            testResult: new TestRunResult(
                false,
                "/workspace/project/.agent/runs/issue-42/tests.log",
                new[] { new TestCommandResult("dotnet test", 1, string.Empty, "tests failed", TimeSpan.Zero) },
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow,
                "Command failed (exit 1): dotnet test"));

        var result = services.Execute(new[] { "run-once" });

        Assert.NotEqual(0, result.ExitCode);
        Assert.Equal(MissionStatus.TestsFailed, services.Missions.Saved.Last().Status);
    }

    [Fact]
    public void RunOnceMarksMissionAsFailedWhenSubmitFails()
    {
        var gitRepo = new RecordingGitRepository(currentBranch: "main", pushThrows: true);
        var services = RunOnceTestServices.Valid(gitRepo: gitRepo);

        var result = services.Execute(new[] { "run-once" });

        Assert.NotEqual(0, result.ExitCode);
        Assert.Equal(MissionStatus.Failed, services.Missions.Saved.Last().Status);
    }

    [Fact]
    public void RunOnceMarksWorkItemAsWorking()
    {
        var services = RunOnceTestServices.Valid();

        var result = services.Execute(new[] { "run-once" });

        Assert.Equal(0, result.ExitCode);
        Assert.True(services.WorkItems.WorkingMarked);
    }
}

internal sealed class RunOnceTestServices
{
    private readonly FakeConfigLoader _configLoader;
    private readonly FakePlanningAgent _planningAgent;
    private readonly FakeCodingAgent _codingAgent;
    private readonly FakeTestRunner _testRunner;
    private readonly FakeSafetyChecker _safetyChecker;
    private readonly IMissionBranchPreparer _branchPreparer;

    private RunOnceTestServices(
        FakeConfigLoader configLoader,
        FakeWorkItemProvider workItems,
        FakePlanningAgent planningAgent,
        FakeCodingAgent codingAgent,
        FakeTestRunner testRunner,
        FakeSafetyChecker safetyChecker,
        RecordingGitRepository gitRepo,
        FakePullRequestProvider pullRequests,
        FakeMissionRepository missions,
        IMissionBranchPreparer branchPreparer)
    {
        _configLoader = configLoader;
        WorkItems = workItems;
        _planningAgent = planningAgent;
        _codingAgent = codingAgent;
        _testRunner = testRunner;
        _safetyChecker = safetyChecker;
        GitRepo = gitRepo;
        PullRequests = pullRequests;
        Missions = missions;
        _branchPreparer = branchPreparer;
    }

    public FakeWorkItemProvider WorkItems { get; }
    public RecordingGitRepository GitRepo { get; }
    public FakePullRequestProvider PullRequests { get; }
    public FakeMissionRepository Missions { get; }

    public static RunOnceTestServices Valid(
        RecordingGitRepository? gitRepo = null,
        PlanningResult? planningResult = null,
        CodingResult? codingResult = null,
        TestRunResult? testResult = null,
        SafetyCheckResult? safetyResult = null,
        bool prSucceeds = true,
        bool hasReadyItems = true,
        string repoPath = "/workspace/project",
        IMissionBranchPreparer? branchPreparer = null)
    {
        var config = new AgentForemanConfig
        {
            Project = new ProjectConfig
            {
                Name = "project",
                RepoPath = repoPath,
                DefaultBranch = "main",
            },
            WorkItems = new WorkItemsConfig
            {
                Provider = "github",
                Repo = "caio/elevator-ads-mvp",
                ReviewLabel = "agent-review",
                WorkingLabel = "agent-working",
                ReadyLabel = "agent-ready",
            },
            Planner = new PlannerConfig
            {
                Provider = "claude-cli",
                Command = "claude",
            },
            Executor = new ExecutorConfig
            {
                Provider = "codex-cli",
                Command = "codex",
                Sandbox = "workspace-write",
                Approval = "never",
            },
            Tests = new TestsConfig
            {
                Commands = new[] { "dotnet test" },
            },
            Safety = new SafetyConfig
            {
                MaxFilesChanged = 25,
                ForbiddenPaths = new[] { ".env", "secrets/" },
            },
            Quota = new QuotaConfig
            {
                RetryAfterHours = 5,
                QuotaPatterns = new[] { "usage limit", "rate limit", "quota exceeded" },
            },
            Database = new DatabaseConfig
            {
                Provider = "postgresql",
                ConnectionString = "Host=localhost;Database=agent_foreman",
            },
        };

        IReadOnlyList<WorkItem> readyItems = hasReadyItems
            ? new[]
            {
                new WorkItem(
                    "42",
                    WorkItemSource.GitHub,
                    "Fix elevator ad pacing",
                    "Issue body",
                    "https://github.com/caio/elevator-ads-mvp/issues/42",
                    "caio/elevator-ads-mvp",
                    new[] { new WorkItemLabel("agent-ready") },
                    DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow),
            }
            : Array.Empty<WorkItem>();

        var defaultPlanResult = PlanningResult.Succeeded(
            planPath: Path.Combine(repoPath, ".agent", "runs", "issue-42", "plan.md"),
            logPath: Path.Combine(repoPath, ".agent", "runs", "issue-42", "claude-plan.log"),
            stdout: "# Plan content",
            stderr: string.Empty,
            exitCode: 0,
            startedAt: DateTimeOffset.UtcNow,
            finishedAt: DateTimeOffset.UtcNow);

        var defaultCodingResult = CodingResult.Succeeded(
            logPath: Path.Combine(repoPath, ".agent", "runs", "issue-42", "codex-exec.log"),
            stdout: "ok",
            stderr: string.Empty,
            exitCode: 0,
            startedAt: DateTimeOffset.UtcNow,
            finishedAt: DateTimeOffset.UtcNow);

        var defaultTestResult = new TestRunResult(
            true,
            Path.Combine(repoPath, ".agent", "runs", "issue-42", "tests.log"),
            new[] { new TestCommandResult("dotnet test", 0, "All tests passed.", string.Empty, TimeSpan.Zero) },
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            null);

        return new RunOnceTestServices(
            new FakeConfigLoader(AgentForemanConfigLoadResult.Success(config)),
            new FakeWorkItemProvider(exists: true, readyItems: readyItems),
            new FakePlanningAgent(planningResult ?? defaultPlanResult),
            new FakeCodingAgent(codingResult ?? defaultCodingResult),
            new FakeTestRunner(testResult ?? defaultTestResult),
            new FakeSafetyChecker(safetyResult ?? SafetyCheckResult.Ok()),
            gitRepo ?? new RecordingGitRepository(currentBranch: "main"),
            new FakePullRequestProvider(prSucceeds),
            new FakeMissionRepository(),
            branchPreparer ?? new FakeMissionBranchPreparer());
    }

    public CommandResult Execute(IReadOnlyList<string> args)
    {
        var doctorServices = DoctorTestServices.Valid();
        return CliApplication.Execute(
            args,
            _configLoader,
            doctorServices.RepositoryChecker,
            doctorServices.CommandChecker,
            new FakeStateStore(),
            new RecordingCommandRunner(),
            workItemProvider: WorkItems,
            planningAgent: _planningAgent,
            missionRepository: Missions,
            codingAgent: _codingAgent,
            testRunner: _testRunner,
            safetyChecker: _safetyChecker,
            gitRepository: GitRepo,
            pullRequestProvider: PullRequests,
            branchPreparer: _branchPreparer);
    }
}
