using AgentForeman.Cli;
using AgentForeman.Core.Configuration;
using AgentForeman.Core.State;

namespace AgentForeman.Tests;

public sealed class CliCancelTests
{
    [Fact]
    public void CancelReturnsNonZeroWhenMissionIsNotFound()
    {
        var services = CancelTestServices.Valid();

        var result = services.Execute(new[] { "cancel", "42" });

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("not found", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CancelNumericIdResolvesExternalWorkItemId()
    {
        var mission = MakeMission("42", MissionStatus.PlanReady);
        var services = CancelTestServices.Valid(mission: mission);

        services.Execute(new[] { "cancel", "42" });

        Assert.Equal("42", services.WorkItems.RequestedId);
    }

    [Fact]
    public void CancelIssueIdResolvesMissionId()
    {
        var mission = MakeMission("42", MissionStatus.PlanReady);
        var services = CancelTestServices.Valid(mission: mission);

        var result = services.Execute(new[] { "cancel", "issue-42" });

        Assert.Equal(0, result.ExitCode);
        Assert.Equal(MissionStatus.Cancelled, services.Missions.Saved.Last().Status);
    }

    [Fact]
    public void CancelMarksMissionAsCancelled()
    {
        var mission = MakeMission("42", MissionStatus.PausedQuota);
        var services = CancelTestServices.Valid(mission: mission);

        var result = services.Execute(new[] { "cancel", "42" });

        Assert.Equal(0, result.ExitCode);
        Assert.Equal(MissionStatus.Cancelled, services.Missions.Saved.Last().Status);
        Assert.Contains($"Mission cancelled: {mission.Id}", result.Output);
    }

    [Fact]
    public void CancelStoresCancellationReason()
    {
        var mission = MakeMission("42", MissionStatus.PlanReady);
        var services = CancelTestServices.Valid(mission: mission);

        services.Execute(new[] { "cancel", "42", "--reason", "no longer needed" });

        Assert.Equal("no longer needed", services.Missions.Saved.Last().LastError);
    }

    [Fact]
    public void CancelUsesDefaultReasonWhenNotSpecified()
    {
        var mission = MakeMission("42", MissionStatus.PlanReady);
        var services = CancelTestServices.Valid(mission: mission);

        services.Execute(new[] { "cancel", "42" });

        Assert.Equal("Cancelled by user.", services.Missions.Saved.Last().LastError);
    }

    [Fact]
    public void CancelCommentsOnWorkItem()
    {
        var mission = MakeMission("42", MissionStatus.PlanReady);
        var services = CancelTestServices.Valid(mission: mission);

        services.Execute(new[] { "cancel", "42", "--reason", "deadline passed" });

        Assert.NotNull(services.WorkItems.LastComment);
        Assert.Contains("Mission cancelled", services.WorkItems.LastComment);
        Assert.Contains("deadline passed", services.WorkItems.LastComment);
    }

    [Fact]
    public void CancelAlreadyCancelledReturnsZero()
    {
        var mission = MakeMission("42", MissionStatus.Cancelled);
        var services = CancelTestServices.Valid(mission: mission);

        var result = services.Execute(new[] { "cancel", "42" });

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("already cancelled", result.Output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CancelPullRequestCreatedReturnsNonZeroAndDoesNotChangeStatus()
    {
        var mission = MakeMission("42", MissionStatus.PullRequestCreated,
            pullRequestUrl: "https://github.com/caio/elevator-ads-mvp/pull/10");
        var services = CancelTestServices.Valid(mission: mission);

        var result = services.Execute(new[] { "cancel", "42" });

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("pull request", result.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(MissionStatus.PullRequestCreated, services.Missions.Saved.Last().Status);
    }

    [Fact]
    public void CancelDoesNotDeleteFilesOrCallGitReset()
    {
        var mission = MakeMission("42", MissionStatus.PlanReady);
        var gitRepo = new RecordingGitRepository();
        var services = CancelTestServices.Valid(mission: mission, gitRepo: gitRepo);

        var result = services.Execute(new[] { "cancel", "42" });

        Assert.Equal(0, result.ExitCode);
        Assert.False(gitRepo.AddAllCalled);
        Assert.Null(gitRepo.CommitMessage);
        Assert.Null(gitRepo.PushedBranch);
    }

    [Fact]
    public void CancelDoesNotRemoveLabels()
    {
        var mission = MakeMission("42", MissionStatus.PlanReady);
        var services = CancelTestServices.Valid(mission: mission);

        var result = services.Execute(new[] { "cancel", "42" });

        Assert.Equal(0, result.ExitCode);
        Assert.False(services.WorkItems.MarkedAsReview);
        Assert.Null(services.WorkItems.PausedReason);
    }

    private static Mission MakeMission(
        string externalId,
        MissionStatus status,
        string? pullRequestUrl = null)
    {
        return new Mission(
            $"github-{externalId}",
            externalId,
            "GitHub",
            "Fix elevator ad pacing",
            status,
            Branch: null,
            PlanPath: null,
            PullRequestUrl: pullRequestUrl,
            RetryAfter: null,
            LastError: null,
            CreatedAt: DateTimeOffset.UtcNow,
            UpdatedAt: DateTimeOffset.UtcNow);
    }
}

internal sealed class CancelTestServices
{
    private readonly FakeConfigLoader _configLoader;
    private readonly RecordingGitRepository _gitRepo;

    private CancelTestServices(
        FakeConfigLoader configLoader,
        FakeWorkItemProvider workItems,
        FakeMissionRepository missions,
        RecordingGitRepository gitRepo)
    {
        _configLoader = configLoader;
        WorkItems = workItems;
        Missions = missions;
        _gitRepo = gitRepo;
    }

    public FakeWorkItemProvider WorkItems { get; }
    public FakeMissionRepository Missions { get; }

    public static CancelTestServices Valid(
        Mission? mission = null,
        bool workItemExists = true,
        RecordingGitRepository? gitRepo = null)
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

        var missions = new FakeMissionRepository();
        if (mission is not null)
            missions.Save(mission);

        return new CancelTestServices(
            new FakeConfigLoader(AgentForemanConfigLoadResult.Success(config)),
            new FakeWorkItemProvider(workItemExists),
            missions,
            gitRepo ?? new RecordingGitRepository());
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
            gitRepository: _gitRepo,
            workItemProvider: WorkItems,
            missionRepository: Missions);
    }
}
