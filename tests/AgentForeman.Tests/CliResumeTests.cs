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

public sealed class CliResumeTests
{
    [Fact]
    public void ResumeReturnsNonZeroWhenMissionIsNotFound()
    {
        var services = ResumeTestServices.Valid();

        var result = services.Execute(new[] { "resume", "42" });

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("Mission not found", result.Error);
    }

    [Fact]
    public void ResumeReturnsNonZeroWhenPausedQuotaRetryAfterIsInFuture()
    {
        var mission = MakeMission("42", MissionStatus.PausedQuota,
            retryAfter: DateTimeOffset.UtcNow.AddHours(2));
        var services = ResumeTestServices.Valid(mission: mission);

        var result = services.Execute(new[] { "resume", "42" });

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("paused until", result.Error);
    }

    [Fact]
    public void ResumeForceRetriesPausedQuotaBeforeRetryAfter()
    {
        var mission = MakeMission("42", MissionStatus.PausedQuota,
            retryAfter: DateTimeOffset.UtcNow.AddHours(2));
        var services = ResumeTestServices.Valid(mission: mission);

        var result = services.Execute(new[] { "resume", "42", "--force" });

        Assert.Equal(0, result.ExitCode);
    }

    [Fact]
    public void ResumePlanReadyRunsExecuteVerifySubmit()
    {
        var mission = MakeMission("42", MissionStatus.PlanReady,
            planPath: "/workspace/project/.agent/runs/issue-42/plan.md");
        var services = ResumeTestServices.Valid(mission: mission);

        var result = services.Execute(new[] { "resume", "42" });

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("[1/3] Executing...", result.Output);
        Assert.Contains("[2/3] Verifying...", result.Output);
        Assert.Contains("[3/3] Submitting...", result.Output);
        Assert.DoesNotContain("Planning...", result.Output);
    }

    [Fact]
    public void ResumeCodingCompletedRunsVerifySubmit()
    {
        var mission = MakeMission("42", MissionStatus.CodingCompleted);
        var services = ResumeTestServices.Valid(mission: mission);

        var result = services.Execute(new[] { "resume", "42" });

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("[1/2] Verifying...", result.Output);
        Assert.Contains("[2/2] Submitting...", result.Output);
        Assert.DoesNotContain("Executing...", result.Output);
    }

    [Fact]
    public void ResumeTestsPassedRunsSubmitOnly()
    {
        var mission = MakeMission("42", MissionStatus.TestsPassed);
        var services = ResumeTestServices.Valid(mission: mission);

        var result = services.Execute(new[] { "resume", "42" });

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("[1/1] Submitting...", result.Output);
        Assert.DoesNotContain("Verifying...", result.Output);
    }

    [Fact]
    public void ResumePullRequestCreatedReturnsZeroAndDoesNotSubmitAgain()
    {
        var mission = MakeMission("42", MissionStatus.PullRequestCreated,
            pullRequestUrl: "https://github.com/caio/elevator-ads-mvp/pull/10");
        var services = ResumeTestServices.Valid(mission: mission);

        var result = services.Execute(new[] { "resume", "42" });

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("already completed", result.Output);
        Assert.Null(services.PullRequests.LastRequest);
    }

    [Fact]
    public void ResumeFailedWithoutForceReturnsNonZero()
    {
        var mission = MakeMission("42", MissionStatus.Failed,
            lastError: "codex crashed");
        var services = ResumeTestServices.Valid(mission: mission);

        var result = services.Execute(new[] { "resume", "42" });

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("failed", result.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("--force", result.Error);
    }

    [Fact]
    public void ResumeFailedWithForceResumesFromPlanWhenPlanIsMissing()
    {
        // No plan.md at fake path → starts from Plan
        var mission = MakeMission("42", MissionStatus.Failed);
        var services = ResumeTestServices.Valid(mission: mission);

        var result = services.Execute(new[] { "resume", "42", "--force" });

        Assert.Equal(0, result.ExitCode);
        Assert.NotNull(services.PlanningAgent.LastRequest);
        Assert.Contains("[1/4] Planning...", result.Output);
    }

    [Fact]
    public void ResumeFailedWithForceResumesFromExecuteWhenPlanExists()
    {
        using var workspace = TemporaryDirectory.Create();
        var runDir = Path.Combine(workspace.Path, ".agent", "runs", "issue-42");
        Directory.CreateDirectory(runDir);
        File.WriteAllText(Path.Combine(runDir, "plan.md"), "# Plan");

        var mission = MakeMission("42", MissionStatus.Failed,
            planPath: Path.Combine(runDir, "plan.md"));
        var services = ResumeTestServices.Valid(workspace.Path, mission: mission);

        var result = services.Execute(new[] { "resume", "42", "--force" });

        Assert.Equal(0, result.ExitCode);
        Assert.Null(services.PlanningAgent.LastRequest);
        Assert.Contains("[1/3] Executing...", result.Output);
    }

    [Fact]
    public void ResumeIncludesPreviousLogsDiffContextWhenReExecuting()
    {
        using var workspace = TemporaryDirectory.Create();
        var runDir = Path.Combine(workspace.Path, ".agent", "runs", "issue-42");
        Directory.CreateDirectory(runDir);
        File.WriteAllText(Path.Combine(runDir, "plan.md"), "# Plan");
        File.WriteAllText(Path.Combine(runDir, "codex-exec.log"), "previous log content");

        var mission = MakeMission("42", MissionStatus.PausedQuota,
            planPath: Path.Combine(runDir, "plan.md"),
            retryAfter: DateTimeOffset.UtcNow.AddHours(-1));
        var services = ResumeTestServices.Valid(workspace.Path, mission: mission);

        services.Execute(new[] { "resume", "42" });

        Assert.NotNull(services.CodingAgent.LastRequest);
        Assert.Contains("previous log content", services.CodingAgent.LastRequest!.PreviousLogs ?? string.Empty);
    }

    [Fact]
    public void ResumeStopsOnFirstFailure()
    {
        var mission = MakeMission("42", MissionStatus.PlanReady,
            planPath: "/workspace/project/.agent/runs/issue-42/plan.md");
        var services = ResumeTestServices.Valid(
            mission: mission,
            codingResult: CodingResult.Failure(
                logPath: "/workspace/project/.agent/runs/issue-42/codex-exec.log",
                stdout: string.Empty,
                stderr: "codex crashed",
                exitCode: 1,
                startedAt: DateTimeOffset.UtcNow,
                finishedAt: DateTimeOffset.UtcNow,
                errorMessage: "codex crashed"));

        var result = services.Execute(new[] { "resume", "42" });

        Assert.NotEqual(0, result.ExitCode);
        Assert.Equal(MissionStatus.Failed, services.Missions.Saved.Last().Status);
        Assert.False(services.TestRunner.Invoked);
    }

    [Fact]
    public void ResumeHandlesQuotaPauseAgain()
    {
        var mission = MakeMission("42", MissionStatus.PlanReady,
            planPath: "/workspace/project/.agent/runs/issue-42/plan.md");
        var services = ResumeTestServices.Valid(
            mission: mission,
            codingResult: CodingResult.Failure(
                logPath: "/workspace/project/.agent/runs/issue-42/codex-exec.log",
                stdout: string.Empty,
                stderr: string.Empty,
                exitCode: 1,
                startedAt: DateTimeOffset.UtcNow,
                finishedAt: DateTimeOffset.UtcNow,
                errorMessage: "quota exceeded",
                quotaDetected: true));

        var result = services.Execute(new[] { "resume", "42" });

        Assert.NotEqual(0, result.ExitCode);
        Assert.Equal(MissionStatus.PausedQuota, services.Missions.Saved.Last().Status);
        Assert.NotNull(services.Missions.Saved.Last().RetryAfter);
    }

    private static Mission MakeMission(
        string externalId,
        MissionStatus status,
        string? planPath = null,
        string? pullRequestUrl = null,
        string? lastError = null,
        DateTimeOffset? retryAfter = null)
    {
        return new Mission(
            $"github-{externalId}",
            externalId,
            "GitHub",
            "Fix elevator ad pacing",
            status,
            Branch: null,
            PlanPath: planPath,
            PullRequestUrl: pullRequestUrl,
            RetryAfter: retryAfter,
            LastError: lastError,
            CreatedAt: DateTimeOffset.UtcNow,
            UpdatedAt: DateTimeOffset.UtcNow);
    }
}

internal sealed class ResumeTestServices
{
    private readonly FakeConfigLoader _configLoader;
    private readonly IMissionBranchPreparer _branchPreparer;

    private ResumeTestServices(
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
        PlanningAgent = planningAgent;
        CodingAgent = codingAgent;
        TestRunner = testRunner;
        GitRepo = gitRepo;
        PullRequests = pullRequests;
        Missions = missions;
        _branchPreparer = branchPreparer;
    }

    public FakeWorkItemProvider WorkItems { get; }
    public FakePlanningAgent PlanningAgent { get; }
    public FakeCodingAgent CodingAgent { get; }
    public FakeTestRunner TestRunner { get; }
    public RecordingGitRepository GitRepo { get; }
    public FakePullRequestProvider PullRequests { get; }
    public FakeMissionRepository Missions { get; }

    public static ResumeTestServices Valid(
        string repoPath = "/workspace/project",
        Mission? mission = null,
        PlanningResult? planningResult = null,
        CodingResult? codingResult = null,
        TestRunResult? testResult = null,
        SafetyCheckResult? safetyResult = null,
        RecordingGitRepository? gitRepo = null,
        bool prSucceeds = true,
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

        var missions = new FakeMissionRepository();
        if (mission is not null)
            missions.Save(mission);

        return new ResumeTestServices(
            new FakeConfigLoader(AgentForemanConfigLoadResult.Success(config)),
            new FakeWorkItemProvider(exists: true),
            new FakePlanningAgent(planningResult ?? defaultPlanResult),
            new FakeCodingAgent(codingResult ?? defaultCodingResult),
            new FakeTestRunner(testResult ?? defaultTestResult),
            new FakeSafetyChecker(safetyResult ?? SafetyCheckResult.Ok()),
            gitRepo ?? new RecordingGitRepository(currentBranch: "main"),
            new FakePullRequestProvider(prSucceeds),
            missions,
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
            planningAgent: PlanningAgent,
            missionRepository: Missions,
            codingAgent: CodingAgent,
            testRunner: TestRunner,
            safetyChecker: new FakeSafetyChecker(SafetyCheckResult.Ok()),
            gitRepository: GitRepo,
            pullRequestProvider: PullRequests,
            branchPreparer: _branchPreparer);
    }
}
