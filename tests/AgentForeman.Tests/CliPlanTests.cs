using AgentForeman.Cli;
using AgentForeman.Core.Configuration;
using AgentForeman.Core.Planning;
using AgentForeman.Core.State;
using AgentForeman.Core.WorkItems;

namespace AgentForeman.Tests;

public sealed class CliPlanTests
{
    [Fact]
    public void PlanCommandFetchesWorkItemById()
    {
        var services = PlanTestServices.Valid();

        var result = services.Execute(new[] { "plan", "42" });

        Assert.Equal(0, result.ExitCode);
        Assert.Equal("42", services.WorkItems.RequestedId);
    }

    [Fact]
    public void SuccessfulPlanningMarksMissionAsPlanReady()
    {
        var services = PlanTestServices.Valid();

        var result = services.Execute(new[] { "plan", "42" });

        Assert.Equal(0, result.ExitCode);
        Assert.Equal(MissionStatus.PlanReady, services.Missions.Saved.Last().Status);
        Assert.Contains("Plan created:", result.Output);
    }

    [Fact]
    public void FailedPlanningMarksMissionAsFailed()
    {
        var services = PlanTestServices.Valid(planningResult: PlanningResult.Failure(
            PlanPath: "/workspace/project/.agent/runs/issue-42/plan.md",
            LogPath: "/workspace/project/.agent/runs/issue-42/claude-plan.log",
            Stdout: "",
            Stderr: "planner failed",
            ExitCode: 1,
            StartedAt: DateTimeOffset.UtcNow,
            FinishedAt: DateTimeOffset.UtcNow,
            ErrorMessage: "planner failed"));

        var result = services.Execute(new[] { "plan", "42" });

        Assert.NotEqual(0, result.ExitCode);
        Assert.Equal(MissionStatus.Failed, services.Missions.Saved.Last().Status);
        Assert.Contains("planner failed", services.Missions.Saved.Last().LastError);
    }

    [Fact]
    public void QuotaDetectionMarksMissionAsPausedQuota()
    {
        var services = PlanTestServices.Valid(planningResult: PlanningResult.Failure(
            PlanPath: "/workspace/project/.agent/runs/issue-42/plan.md",
            LogPath: "/workspace/project/.agent/runs/issue-42/claude-plan.log",
            Stdout: "usage limit reached",
            Stderr: "",
            ExitCode: 1,
            StartedAt: DateTimeOffset.UtcNow,
            FinishedAt: DateTimeOffset.UtcNow,
            ErrorMessage: "usage limit reached"));

        var result = services.Execute(new[] { "plan", "42" });

        Assert.NotEqual(0, result.ExitCode);
        Assert.Equal(MissionStatus.PausedQuota, services.Missions.Saved.Last().Status);
        Assert.NotNull(services.Missions.Saved.Last().RetryAfter);
        Assert.Contains("quota", services.WorkItems.PausedReason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PlanCommandReturnsNonZeroWhenWorkItemDoesNotExist()
    {
        var services = PlanTestServices.Valid(workItemExists: false);

        var result = services.Execute(new[] { "plan", "404" });

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("Work item not found", result.Error);
    }
}

internal sealed class PlanTestServices
{
    private readonly FakeConfigLoader _configLoader;
    private readonly FakePlanningAgent _planningAgent;

    private PlanTestServices(
        FakeConfigLoader configLoader,
        FakeWorkItemProvider workItems,
        FakePlanningAgent planningAgent,
        FakeMissionRepository missions)
    {
        _configLoader = configLoader;
        WorkItems = workItems;
        _planningAgent = planningAgent;
        Missions = missions;
    }

    public FakeWorkItemProvider WorkItems { get; }
    public FakeMissionRepository Missions { get; }

    public static PlanTestServices Valid(PlanningResult? planningResult = null, bool workItemExists = true)
    {
        var config = new AgentForemanConfig
        {
            Project = new ProjectConfig
            {
                Name = "project",
                RepoPath = "/workspace/project",
                DefaultBranch = "main",
            },
            WorkItems = new WorkItemsConfig
            {
                Provider = "github",
                Repo = "caio/elevator-ads-mvp",
            },
            Planner = new PlannerConfig
            {
                Provider = "claude-cli",
                Command = "claude",
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

        var planPath = Path.Combine(config.Project.RepoPath, ".agent", "runs", "issue-42", "plan.md");
        return new PlanTestServices(
            new FakeConfigLoader(AgentForemanConfigLoadResult.Success(config)),
            new FakeWorkItemProvider(workItemExists),
            new FakePlanningAgent(planningResult ?? PlanningResult.Succeeded(
                planPath,
                Path.Combine(config.Project.RepoPath, ".agent", "runs", "issue-42", "claude-plan.log"),
                "# Plan",
                "",
                0,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow)),
            new FakeMissionRepository());
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
            missionRepository: Missions);
    }
}

internal sealed class FakeWorkItemProvider : IWorkItemProvider
{
    private readonly bool _exists;
    private readonly IReadOnlyList<WorkItem> _readyItems;

    public FakeWorkItemProvider(bool exists, IReadOnlyList<WorkItem>? readyItems = null)
    {
        _exists = exists;
        _readyItems = readyItems ?? Array.Empty<WorkItem>();
    }

    public string? RequestedId { get; private set; }
    public string? PausedReason { get; private set; }
    public string? LastComment { get; private set; }
    public bool MarkedAsReview { get; private set; }
    public bool WorkingMarked { get; private set; }
    public bool CreateInvoked { get; private set; }

    public Task<IReadOnlyList<WorkItem>> GetReadyItemsAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(_readyItems);
    }

    public Task<WorkItem> GetWorkItemAsync(string externalId, CancellationToken cancellationToken)
    {
        RequestedId = externalId;
        if (!_exists)
        {
            throw new KeyNotFoundException($"Work item not found: {externalId}");
        }

        return Task.FromResult(new WorkItem(
            externalId,
            WorkItemSource.GitHub,
            "Fix elevator ad pacing",
            "Issue body",
            "https://github.com/caio/elevator-ads-mvp/issues/" + externalId,
            "caio/elevator-ads-mvp",
            new[] { new WorkItemLabel("agent-ready") },
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow));
    }

    public Task MarkAsWorkingAsync(WorkItem item, CancellationToken cancellationToken)
    {
        WorkingMarked = true;
        return Task.CompletedTask;
    }

    public Task MarkAsPausedAsync(WorkItem item, string reason, DateTimeOffset retryAfter, CancellationToken cancellationToken)
    {
        PausedReason = reason;
        return Task.CompletedTask;
    }

    public Task MarkAsReviewAsync(WorkItem item, string pullRequestUrl, CancellationToken cancellationToken)
    {
        MarkedAsReview = true;
        return Task.CompletedTask;
    }

    public Task AddCommentAsync(WorkItem item, string comment, CancellationToken cancellationToken)
    {
        LastComment = comment;
        return Task.CompletedTask;
    }

    public Task<WorkItem> CreateWorkItemAsync(CreateWorkItemRequest request, CancellationToken cancellationToken)
    {
        CreateInvoked = true;
        throw new NotImplementedException();
    }
}

internal sealed class FakePlanningAgent : IPlanningAgent
{
    private readonly PlanningResult _result;

    public FakePlanningAgent(PlanningResult result)
    {
        _result = result;
    }

    public PlanningRequest? LastRequest { get; private set; }

    public Task<PlanningResult> CreatePlanAsync(PlanningRequest request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        return Task.FromResult(_result);
    }
}

internal sealed class FakeMissionRepository : IMissionRepository
{
    private readonly Dictionary<string, Mission> _missions = new();

    public List<Mission> Saved { get; } = new();
    public int? LastRecentLimit { get; private set; }

    public Mission? GetById(string id)
    {
        return _missions.TryGetValue(id, out var mission) ? mission : null;
    }

    public void Save(Mission mission)
    {
        _missions[mission.Id] = mission;
        Saved.Add(mission);
    }

    public IReadOnlyList<Mission> GetRecent(int limit)
    {
        LastRecentLimit = limit;
        return _missions.Values
            .OrderByDescending(m => m.UpdatedAt)
            .Take(limit)
            .ToList();
    }

    public IReadOnlyList<Mission> GetByStatus(MissionStatus status, int limit)
    {
        return _missions.Values
            .Where(m => m.Status == status)
            .OrderByDescending(m => m.UpdatedAt)
            .Take(limit)
            .ToList();
    }
}

internal sealed class TemporaryDirectory : IDisposable
{
    private TemporaryDirectory(string path)
    {
        Path = path;
    }

    public string Path { get; }

    public static TemporaryDirectory Create()
    {
        return new TemporaryDirectory(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "agent-foreman-" + Guid.NewGuid().ToString("N")));
    }

    public void Dispose()
    {
        if (Directory.Exists(Path))
        {
            Directory.Delete(Path, recursive: true);
        }
    }
}
