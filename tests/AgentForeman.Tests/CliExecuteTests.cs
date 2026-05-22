using AgentForeman.Cli;
using AgentForeman.Core.Coding;
using AgentForeman.Core.Configuration;
using AgentForeman.Core.Git;
using AgentForeman.Core.State;

namespace AgentForeman.Tests;

public sealed class CliExecuteTests
{
    [Fact]
    public void ExecuteCommandFetchesWorkItemById()
    {
        using var workspace = TemporaryDirectory.Create();
        var services = ExecuteTestServices.Valid(workspace.Path);
        SeedPlan(workspace.Path, "42");

        var result = services.Execute(new[] { "execute", "42" });

        Assert.Equal(0, result.ExitCode);
        Assert.Equal("42", services.WorkItems.RequestedId);
    }

    [Fact]
    public void ExecuteReturnsNonZeroWhenPlanIsMissing()
    {
        using var workspace = TemporaryDirectory.Create();
        var services = ExecuteTestServices.Valid(workspace.Path);

        var result = services.Execute(new[] { "execute", "42" });

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("Plan not found", result.Error);
        Assert.Contains("agent-foreman plan 42", result.Error);
        Assert.False(services.CodingAgent.Invoked);
    }

    [Fact]
    public void SuccessfulExecutionMarksMissionAsCodingCompleted()
    {
        using var workspace = TemporaryDirectory.Create();
        var services = ExecuteTestServices.Valid(workspace.Path);
        SeedPlan(workspace.Path, "42");

        var result = services.Execute(new[] { "execute", "42" });

        Assert.Equal(0, result.ExitCode);
        Assert.Equal(MissionStatus.CodingCompleted, services.Missions.Saved.Last().Status);
        Assert.Contains("Execution completed.", result.Output);
        Assert.Contains("codex-exec.log", result.Output);
    }

    [Fact]
    public void FailedExecutionMarksMissionAsFailed()
    {
        using var workspace = TemporaryDirectory.Create();
        var services = ExecuteTestServices.Valid(
            workspace.Path,
            codingResult: CodingResult.Failure(
                logPath: Path.Combine(workspace.Path, ".agent", "runs", "issue-42", "codex-exec.log"),
                stdout: string.Empty,
                stderr: "codex failed",
                exitCode: 1,
                startedAt: DateTimeOffset.UtcNow,
                finishedAt: DateTimeOffset.UtcNow,
                errorMessage: "codex failed"));
        SeedPlan(workspace.Path, "42");

        var result = services.Execute(new[] { "execute", "42" });

        Assert.NotEqual(0, result.ExitCode);
        Assert.Equal(MissionStatus.Failed, services.Missions.Saved.Last().Status);
        Assert.Contains("codex failed", services.Missions.Saved.Last().LastError);
    }

    [Fact]
    public void QuotaDetectionMarksMissionAsPausedQuota()
    {
        using var workspace = TemporaryDirectory.Create();
        var services = ExecuteTestServices.Valid(
            workspace.Path,
            codingResult: CodingResult.Failure(
                logPath: Path.Combine(workspace.Path, ".agent", "runs", "issue-42", "codex-exec.log"),
                stdout: "usage limit reached",
                stderr: string.Empty,
                exitCode: 1,
                startedAt: DateTimeOffset.UtcNow,
                finishedAt: DateTimeOffset.UtcNow,
                errorMessage: "usage limit reached",
                quotaDetected: true));
        SeedPlan(workspace.Path, "42");

        var result = services.Execute(new[] { "execute", "42" });

        Assert.NotEqual(0, result.ExitCode);
        Assert.Equal(MissionStatus.PausedQuota, services.Missions.Saved.Last().Status);
        Assert.NotNull(services.Missions.Saved.Last().RetryAfter);
        Assert.Contains("quota", services.WorkItems.LastComment, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ExecuteCommandDoesNotCommitPushOrCreatePullRequest()
    {
        using var workspace = TemporaryDirectory.Create();
        var services = ExecuteTestServices.Valid(workspace.Path);
        SeedPlan(workspace.Path, "42");

        var result = services.Execute(new[] { "execute", "42" });

        Assert.Equal(0, result.ExitCode);
        Assert.False(services.WorkItems.MarkedAsReview);
        Assert.False(services.WorkItems.CreateInvoked);
        Assert.Null(services.Missions.Saved.Last().PullRequestUrl);
    }

    [Fact]
    public void ExecuteCommandReturnsNonZeroWhenWorkItemDoesNotExist()
    {
        using var workspace = TemporaryDirectory.Create();
        var services = ExecuteTestServices.Valid(workspace.Path, workItemExists: false);

        var result = services.Execute(new[] { "execute", "404" });

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("Work item not found", result.Error);
    }

    private static void SeedPlan(string repoPath, string externalId)
    {
        var planDir = Path.Combine(repoPath, ".agent", "runs", $"issue-{externalId}");
        Directory.CreateDirectory(planDir);
        File.WriteAllText(Path.Combine(planDir, "plan.md"), "# Saved plan");
    }
}

internal sealed class ExecuteTestServices
{
    private readonly FakeConfigLoader _configLoader;
    private readonly IMissionBranchPreparer _branchPreparer;

    private ExecuteTestServices(
        FakeConfigLoader configLoader,
        FakeWorkItemProvider workItems,
        FakeCodingAgent codingAgent,
        FakeMissionRepository missions,
        IMissionBranchPreparer branchPreparer)
    {
        _configLoader = configLoader;
        WorkItems = workItems;
        CodingAgent = codingAgent;
        Missions = missions;
        _branchPreparer = branchPreparer;
    }

    public FakeWorkItemProvider WorkItems { get; }
    public FakeCodingAgent CodingAgent { get; }
    public FakeMissionRepository Missions { get; }

    public static ExecuteTestServices Valid(
        string repoPath,
        CodingResult? codingResult = null,
        bool workItemExists = true,
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
            },
            Executor = new ExecutorConfig
            {
                Provider = "codex-cli",
                Command = "codex",
                Sandbox = "workspace-write",
                Approval = "never",
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

        var defaultResult = CodingResult.Succeeded(
            logPath: Path.Combine(repoPath, ".agent", "runs", "issue-42", "codex-exec.log"),
            stdout: "ok",
            stderr: string.Empty,
            exitCode: 0,
            startedAt: DateTimeOffset.UtcNow,
            finishedAt: DateTimeOffset.UtcNow);

        return new ExecuteTestServices(
            new FakeConfigLoader(AgentForemanConfigLoadResult.Success(config)),
            new FakeWorkItemProvider(workItemExists),
            new FakeCodingAgent(codingResult ?? defaultResult),
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
            missionRepository: Missions,
            codingAgent: CodingAgent,
            branchPreparer: _branchPreparer);
    }
}

internal sealed class FakeCodingAgent : ICodingAgent
{
    private readonly CodingResult _result;

    public FakeCodingAgent(CodingResult result)
    {
        _result = result;
    }

    public CodingRequest? LastRequest { get; private set; }
    public bool Invoked => LastRequest is not null;

    public Task<CodingResult> ExecuteAsync(CodingRequest request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        return Task.FromResult(_result);
    }
}
