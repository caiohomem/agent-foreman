using AgentForeman.Cli;
using AgentForeman.Core.Configuration;
using AgentForeman.Core.Events;
using AgentForeman.Core.State;
using AgentForeman.Core.WorkItems;

namespace AgentForeman.Tests;

public sealed class CliSyncTests
{
    [Fact]
    public void SyncMarksPullRequestCreatedMissionAsCompletedWhenIssueIsClosed()
    {
        var services = SyncTestServices.Valid(WorkItemState.Closed);

        var result = services.Execute(new[] { "sync" });

        Assert.Equal(0, result.ExitCode);
        Assert.Equal(MissionStatus.Completed, services.Missions.Saved.Last().Status);
        Assert.True(services.WorkItems.CompletedMarked);
        Assert.Contains(services.Events.Events, e => e.EventType == MissionEventType.MissionCompleted);
    }

    [Fact]
    public void SyncDoesNotMarkMissionCompletedWhenIssueIsOpen()
    {
        var services = SyncTestServices.Valid(WorkItemState.Open);

        var result = services.Execute(new[] { "sync" });

        Assert.Equal(0, result.ExitCode);
        Assert.Equal(MissionStatus.PullRequestCreated, services.Missions.GetById("github-42")!.Status);
        Assert.False(services.WorkItems.CompletedMarked);
        Assert.Contains("keep waiting for review", result.Output);
    }

    [Fact]
    public void SyncDryRunDoesNotUpdateDatabase()
    {
        var services = SyncTestServices.Valid(WorkItemState.Closed);

        var result = services.Execute(new[] { "sync", "--dry-run" });

        Assert.Equal(0, result.ExitCode);
        Assert.Empty(services.Missions.Saved.Skip(1));
    }

    [Fact]
    public void SyncDryRunDoesNotUpdateLabels()
    {
        var services = SyncTestServices.Valid(WorkItemState.Closed);

        var result = services.Execute(new[] { "sync", "--dry-run" });

        Assert.Equal(0, result.ExitCode);
        Assert.False(services.WorkItems.CompletedMarked);
    }
}

internal sealed class SyncTestServices
{
    private readonly FakeConfigLoader _configLoader;

    private SyncTestServices(FakeConfigLoader configLoader, FakeMissionRepository missions, FakeWorkItemProvider workItems, FakeMissionEventRecorder events)
    {
        _configLoader = configLoader;
        Missions = missions;
        WorkItems = workItems;
        Events = events;
    }

    public FakeMissionRepository Missions { get; }
    public FakeWorkItemProvider WorkItems { get; }
    public FakeMissionEventRecorder Events { get; }

    public static SyncTestServices Valid(WorkItemState itemState)
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
                ReviewLabel = "agent-review",
                WorkingLabel = "agent-working",
                ReadyLabel = "agent-ready",
                PausedLabel = "agent-paused",
                BlockedLabel = "agent-blocked",
                FailedLabel = "agent-failed",
            },
            Database = new DatabaseConfig
            {
                Provider = "postgresql",
                ConnectionString = "Host=localhost;Database=agent_foreman",
            },
        };

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

        var workItems = new FakeWorkItemProvider(exists: true, itemState: itemState);

        return new SyncTestServices(
            new FakeConfigLoader(AgentForemanConfigLoadResult.Success(config)),
            missions,
            workItems,
            new FakeMissionEventRecorder());
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
            missionRepository: Missions,
            workItemProvider: WorkItems,
            missionEventRecorder: Events);
    }
}
