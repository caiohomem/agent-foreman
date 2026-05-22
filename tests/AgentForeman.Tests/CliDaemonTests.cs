using AgentForeman.Cli;
using AgentForeman.Core.Coding;
using AgentForeman.Core.Configuration;
using AgentForeman.Core.Git;
using AgentForeman.Core.Orchestration;
using AgentForeman.Core.Planning;
using AgentForeman.Core.Safety;
using AgentForeman.Core.State;
using AgentForeman.Core.Testing;
using AgentForeman.Core.WorkItems;

namespace AgentForeman.Tests;

public sealed class CliDaemonTests
{
    [Fact]
    public void DaemonOnceRunsOneLoopAndExits()
    {
        var orchestrator = new FakeMissionOrchestrator(new DaemonTickResult(false, true, null, null));
        var services = DaemonTestServices.Valid(orchestrator: orchestrator);

        var result = services.Execute(new[] { "daemon", "--once" });

        Assert.Equal(0, result.ExitCode);
        Assert.Equal(1, orchestrator.TickCount);
        Assert.Contains("Checking for work", result.Output);
    }

    [Fact]
    public void DaemonDoesNotProcessNewWorkWhenActiveMissionExists()
    {
        var activeMission = MakeMission("42", MissionStatus.Planning);
        var services = DaemonTestServices.Valid(activeMission: activeMission);

        var result = services.Execute(new[] { "daemon", "--once" });

        Assert.Equal(0, result.ExitCode);
        Assert.False(services.WorkItems.WorkingMarked);
        Assert.Contains("Active mission", result.Output);
    }

    [Fact]
    public void DaemonResumesPausedQuotaMissionWhenRetryAfterHasPassed()
    {
        var pausedMission = MakeMission("42", MissionStatus.PausedQuota,
            retryAfter: DateTimeOffset.UtcNow.AddHours(-1));
        var services = DaemonTestServices.Valid(pausedMission: pausedMission);

        var result = services.Execute(new[] { "daemon", "--once" });

        Assert.Equal(0, result.ExitCode);
        Assert.NotNull(services.PlanningAgent.LastRequest);
    }

    [Fact]
    public void DaemonSkipsPausedQuotaMissionWhenRetryAfterIsInFuture()
    {
        var pausedMission = MakeMission("42", MissionStatus.PausedQuota,
            retryAfter: DateTimeOffset.UtcNow.AddHours(2));
        var services = DaemonTestServices.Valid(pausedMission: pausedMission);

        var result = services.Execute(new[] { "daemon", "--once" });

        Assert.Equal(0, result.ExitCode);
        Assert.Null(services.PlanningAgent.LastRequest);
    }

    [Fact]
    public void DaemonCallsRunOnceWhenNoPausedMissionIsReady()
    {
        var readyItem = new WorkItem(
            "42", WorkItemSource.GitHub, "Fix elevator ad pacing", "body",
            "https://github.com/caio/elevator-ads-mvp/issues/42", "caio/elevator-ads-mvp",
            new[] { new WorkItemLabel("agent-ready") },
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        var services = DaemonTestServices.Valid(readyItem: readyItem);

        var result = services.Execute(new[] { "daemon", "--once" });

        Assert.Equal(0, result.ExitCode);
        Assert.True(services.WorkItems.WorkingMarked);
    }

    [Fact]
    public void DaemonProcessesOnlyOneMissionPerLoop()
    {
        var orchestrator = new FakeMissionOrchestrator(
            new DaemonTickResult(false, false, null, "https://github.com/caio/elevator-ads-mvp/pull/10"));
        var services = DaemonTestServices.Valid(orchestrator: orchestrator);

        services.Execute(new[] { "daemon", "--once" });

        Assert.Equal(1, orchestrator.TickCount);
    }

    [Fact]
    public void DaemonUsesConfiguredPollInterval()
    {
        var orchestrator = new FakeMissionOrchestrator(new DaemonTickResult(false, true, null, null));
        var services = DaemonTestServices.Valid(orchestrator: orchestrator, pollIntervalSeconds: 42);

        var result = services.Execute(new[] { "daemon", "--once" });

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Poll interval: 42 seconds", result.Output);
    }

    [Fact]
    public void DaemonIntervalOverridesConfiguredInterval()
    {
        var orchestrator = new FakeMissionOrchestrator(new DaemonTickResult(false, true, null, null));
        var services = DaemonTestServices.Valid(orchestrator: orchestrator, pollIntervalSeconds: 300);

        var result = services.Execute(new[] { "daemon", "--once", "--interval", "60" });

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Poll interval: 60 seconds", result.Output);
    }

    [Fact]
    public void DaemonStopsGracefullyOnCancellation()
    {
        var orchestrator = new FakeMissionOrchestrator(throwOnTick: true);
        var services = DaemonTestServices.Valid(orchestrator: orchestrator);

        var result = services.Execute(new[] { "daemon" });

        Assert.Equal(0, result.ExitCode);
    }

    private static Mission MakeMission(
        string externalId,
        MissionStatus status,
        DateTimeOffset? retryAfter = null)
    {
        return new Mission(
            $"github-{externalId}",
            externalId,
            "GitHub",
            "Fix elevator ad pacing",
            status,
            Branch: null,
            PlanPath: null,
            PullRequestUrl: null,
            RetryAfter: retryAfter,
            LastError: null,
            CreatedAt: DateTimeOffset.UtcNow,
            UpdatedAt: DateTimeOffset.UtcNow);
    }
}

internal sealed class DaemonTestServices
{
    private readonly FakeConfigLoader _configLoader;
    private readonly IMissionOrchestrator? _orchestrator;
    private readonly IMissionBranchPreparer _branchPreparer;

    private DaemonTestServices(
        FakeConfigLoader configLoader,
        FakeWorkItemProvider workItems,
        FakePlanningAgent planningAgent,
        FakeCodingAgent codingAgent,
        FakeTestRunner testRunner,
        RecordingGitRepository gitRepo,
        FakePullRequestProvider pullRequests,
        FakeMissionRepository missions,
        IMissionOrchestrator? orchestrator,
        IMissionBranchPreparer branchPreparer)
    {
        _configLoader = configLoader;
        _orchestrator = orchestrator;
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

    public static DaemonTestServices Valid(
        string repoPath = "/workspace/project",
        Mission? activeMission = null,
        Mission? pausedMission = null,
        WorkItem? readyItem = null,
        IMissionOrchestrator? orchestrator = null,
        int pollIntervalSeconds = 300,
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
            Daemon = new DaemonConfig
            {
                Enabled = true,
                PollIntervalSeconds = pollIntervalSeconds,
                RunOnStartup = true,
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
        if (activeMission is not null) missions.Save(activeMission);
        if (pausedMission is not null) missions.Save(pausedMission);

        var readyItems = readyItem is not null
            ? (IReadOnlyList<WorkItem>)new[] { readyItem }
            : Array.Empty<WorkItem>();

        return new DaemonTestServices(
            new FakeConfigLoader(AgentForemanConfigLoadResult.Success(config)),
            new FakeWorkItemProvider(exists: true, readyItems: readyItems),
            new FakePlanningAgent(defaultPlanResult),
            new FakeCodingAgent(defaultCodingResult),
            new FakeTestRunner(defaultTestResult),
            new RecordingGitRepository(currentBranch: "main"),
            new FakePullRequestProvider(succeeds: true),
            missions,
            orchestrator,
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
            orchestrator: _orchestrator,
            branchPreparer: _branchPreparer);
    }
}

internal sealed class FakeMissionOrchestrator : IMissionOrchestrator
{
    private readonly DaemonTickResult _tickResult;
    private readonly bool _throwOnTick;

    public FakeMissionOrchestrator(DaemonTickResult? tickResult = null, bool throwOnTick = false)
    {
        _tickResult = tickResult ?? new DaemonTickResult(false, true, null, null);
        _throwOnTick = throwOnTick;
    }

    public int TickCount { get; private set; }

    public Task<RunOnceResult> RunOnceAsync(RunOnceRequest request, Action<string>? onProgress, CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public Task<ResumeResult> ResumeAsync(ResumeRequest request, Action<string>? onProgress, CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public Task<DaemonTickResult> DaemonTickAsync(DaemonTickRequest request, Action<string>? onProgress, CancellationToken cancellationToken)
    {
        TickCount++;
        if (_throwOnTick)
            throw new OperationCanceledException();
        return Task.FromResult(_tickResult);
    }
}
