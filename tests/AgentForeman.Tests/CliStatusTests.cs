using AgentForeman.Cli;
using AgentForeman.Core.Configuration;
using AgentForeman.Core.State;

namespace AgentForeman.Tests;

public sealed class CliStatusTests
{
    [Fact]
    public void StatusReturnsZeroWhenDatabaseIsAvailable()
    {
        var services = StatusTestServices.Valid();

        var result = services.Execute(new[] { "status" });

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Agent Foreman Status", result.Output);
    }

    [Fact]
    public void StatusPrintsNoMissionsFoundWhenEmpty()
    {
        var services = StatusTestServices.Valid();

        var result = services.Execute(new[] { "status" });

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("No missions found", result.Output);
    }

    [Fact]
    public void StatusPrintsMissionSummaryWhenMissionsExist()
    {
        var missions = new FakeMissionRepository();
        missions.Save(new Mission(
            "github-42",
            "42",
            "GitHub",
            "Fix elevator ad pacing",
            MissionStatus.PullRequestCreated,
            Branch: "agent/issue-42",
            PlanPath: null,
            PullRequestUrl: "https://github.com/caio/elevator-ads-mvp/pull/10",
            RetryAfter: null,
            LastError: null,
            CreatedAt: DateTimeOffset.UtcNow,
            UpdatedAt: DateTimeOffset.UtcNow));

        var services = StatusTestServices.Valid(missions: missions);

        var result = services.Execute(new[] { "status" });

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("github-42", result.Output);
        Assert.Contains("Fix elevator ad pacing", result.Output);
        Assert.Contains("PullRequestCreated", result.Output);
        Assert.Contains("agent/issue-42", result.Output);
        Assert.Contains("https://github.com/caio/elevator-ads-mvp/pull/10", result.Output);
    }

    [Fact]
    public void StatusAllDoesNotApplyDefaultLimit()
    {
        var missions = new FakeMissionRepository();
        var services = StatusTestServices.Valid(missions: missions);

        services.Execute(new[] { "status", "--all" });

        Assert.NotEqual(20, missions.LastRecentLimit);
    }

    [Fact]
    public void StatusStatusFiltersByMissionStatus()
    {
        var missions = new FakeMissionRepository();
        missions.Save(new Mission(
            "github-1", "1", "GitHub", "Passed mission",
            MissionStatus.PullRequestCreated,
            Branch: null, PlanPath: null, PullRequestUrl: null,
            RetryAfter: null, LastError: null,
            CreatedAt: DateTimeOffset.UtcNow, UpdatedAt: DateTimeOffset.UtcNow));
        missions.Save(new Mission(
            "github-2", "2", "GitHub", "Failed mission",
            MissionStatus.Failed,
            Branch: null, PlanPath: null, PullRequestUrl: null,
            RetryAfter: null, LastError: "something broke",
            CreatedAt: DateTimeOffset.UtcNow, UpdatedAt: DateTimeOffset.UtcNow));

        var services = StatusTestServices.Valid(missions: missions);

        var result = services.Execute(new[] { "status", "--status", "Failed" });

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Failed mission", result.Output);
        Assert.DoesNotContain("Passed mission", result.Output);
    }

    [Fact]
    public void StatusInvalidStatusReturnsNonZero()
    {
        var services = StatusTestServices.Valid();

        var result = services.Execute(new[] { "status", "--status", "Banana" });

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("Invalid status", result.Error);
        Assert.Contains("Banana", result.Error);
    }

    [Fact]
    public void StatusDatabaseFailureReturnsNonZero()
    {
        var services = StatusTestServices.Valid(missions: new ThrowingMissionRepository());

        var result = services.Execute(new[] { "status" });

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("Database error", result.Error);
    }

    [Fact]
    public void StatusDisplaysCompletedMissionStatus()
    {
        var missions = new FakeMissionRepository();
        missions.Save(new Mission(
            "github-42",
            "42",
            "GitHub",
            "Fix elevator ad pacing",
            MissionStatus.Completed,
            Branch: "agent/issue-42",
            PlanPath: null,
            PullRequestUrl: "https://github.com/caio/elevator-ads-mvp/pull/10",
            RetryAfter: null,
            LastError: null,
            CreatedAt: DateTimeOffset.UtcNow,
            UpdatedAt: DateTimeOffset.UtcNow));

        var services = StatusTestServices.Valid(missions: missions);

        var result = services.Execute(new[] { "status" });

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Completed", result.Output);
    }

    [Fact]
    public void StatusOrdersMissionsByExternalWorkItemIdDescending()
    {
        var missions = new FakeMissionRepository();
        missions.Save(new Mission(
            "github-28",
            "28",
            "GitHub",
            "Older numbered mission",
            MissionStatus.Failed,
            Branch: null,
            PlanPath: null,
            PullRequestUrl: null,
            RetryAfter: null,
            LastError: null,
            CreatedAt: DateTimeOffset.UtcNow,
            UpdatedAt: DateTimeOffset.UtcNow));
        missions.Save(new Mission(
            "github-32",
            "32",
            "GitHub",
            "Newer numbered mission",
            MissionStatus.New,
            Branch: null,
            PlanPath: null,
            PullRequestUrl: null,
            RetryAfter: null,
            LastError: null,
            CreatedAt: DateTimeOffset.UtcNow.AddMinutes(-10),
            UpdatedAt: DateTimeOffset.UtcNow.AddMinutes(-10)));

        var services = StatusTestServices.Valid(missions: missions);

        var result = services.Execute(new[] { "status" });

        Assert.Equal(0, result.ExitCode);
        Assert.True(result.Output.IndexOf("github-32", StringComparison.Ordinal) <
                    result.Output.IndexOf("github-28", StringComparison.Ordinal));
    }
}

internal sealed class StatusTestServices
{
    private readonly FakeConfigLoader _configLoader;
    private readonly IMissionRepository _missions;

    private StatusTestServices(FakeConfigLoader configLoader, IMissionRepository missions)
    {
        _configLoader = configLoader;
        _missions = missions;
    }

    public static StatusTestServices Valid(IMissionRepository? missions = null)
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
            Database = new DatabaseConfig
            {
                Provider = "postgresql",
                ConnectionString = "Host=localhost;Database=agent_foreman",
            },
        };

        return new StatusTestServices(
            new FakeConfigLoader(AgentForemanConfigLoadResult.Success(config)),
            missions ?? new FakeMissionRepository());
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
            missionRepository: _missions);
    }
}

internal sealed class ThrowingMissionRepository : IMissionRepository
{
    public Mission? GetById(string id) =>
        throw new InvalidOperationException("Connection refused");

    public void Save(Mission mission) =>
        throw new InvalidOperationException("Connection refused");

    public IReadOnlyList<Mission> GetRecent(int limit) =>
        throw new InvalidOperationException("Connection refused");

    public IReadOnlyList<Mission> GetByStatus(MissionStatus status, int limit) =>
        throw new InvalidOperationException("Connection refused");
}
