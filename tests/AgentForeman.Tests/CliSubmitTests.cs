using AgentForeman.Cli;
using AgentForeman.Core.Configuration;
using AgentForeman.Core.Events;
using AgentForeman.Core.Git;
using AgentForeman.Core.PullRequests;
using AgentForeman.Core.State;

namespace AgentForeman.Tests;

public sealed class CliSubmitTests
{
    [Fact]
    public void SubmitReturnsNonZeroIfMissionIsNotTestsPassed()
    {
        var services = SubmitTestServices.Valid(missionStatus: MissionStatus.CodingCompleted);

        var result = services.Execute(new[] { "submit", "42" });

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("verify", result.Error);
    }

    [Fact]
    public void SubmitReturnsNonZeroIfNoMissionExists()
    {
        var services = SubmitTestServices.Valid(seedMission: false);

        var result = services.Execute(new[] { "submit", "42" });

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("verify", result.Error);
    }

    [Fact]
    public void SubmitReturnsNonZeroIfNoChangedFilesExist()
    {
        var services = SubmitTestServices.Valid(
            gitRepo: new RecordingGitRepository(currentBranch: "main", hasChanges: false));

        var result = services.Execute(new[] { "submit", "42" });

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("No changes to submit", result.Error);
    }

    [Fact]
    public void SubmitReturnsErrorWhenNotOnExpectedBranch()
    {
        var gitRepo = new RecordingGitRepository(currentBranch: "main");
        var services = SubmitTestServices.Valid(gitRepo: gitRepo);

        var result = services.Execute(new[] { "submit", "42" });

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("agent/issue-42", result.Error);
        Assert.Null(gitRepo.CreatedBranch);
        Assert.Null(gitRepo.CheckedOutBranch);
    }

    [Fact]
    public void SubmitDoesNotPushDirectlyToMain()
    {
        var services = SubmitTestServices.Valid();

        var result = services.Execute(new[] { "submit", "42" });

        Assert.Equal(0, result.ExitCode);
        Assert.NotEqual("main", services.GitRepo.PushedBranch);
        Assert.Equal("agent/issue-42", services.GitRepo.PushedBranch);
    }

    [Fact]
    public void SubmitRunsGitAddCommitAndPush()
    {
        var services = SubmitTestServices.Valid();

        var result = services.Execute(new[] { "submit", "42" });

        Assert.Equal(0, result.ExitCode);
        Assert.True(services.GitRepo.AddAllCalled);
        Assert.Equal("Implement issue #42: Fix elevator ad pacing", services.GitRepo.CommitMessage);
        Assert.Equal("origin", services.GitRepo.PushedRemote);
        Assert.Equal("agent/issue-42", services.GitRepo.PushedBranch);
    }

    [Fact]
    public void SubmitCreatesPullRequestWithExpectedTitleAndBody()
    {
        var services = SubmitTestServices.Valid();

        var result = services.Execute(new[] { "submit", "42" });

        Assert.Equal(0, result.ExitCode);
        Assert.NotNull(services.PullRequests.LastRequest);
        Assert.Equal("Implement issue #42: Fix elevator ad pacing", services.PullRequests.LastRequest!.PullRequestTitle);
        Assert.Contains("Issue #42", services.PullRequests.LastRequest.PullRequestBody);
        Assert.Contains("Closes #42", services.PullRequests.LastRequest.PullRequestBody);
        Assert.Contains("This PR closes #42 when merged.", services.PullRequests.LastRequest.PullRequestBody);
        Assert.Contains("https://github.com/caio/elevator-ads-mvp/issues/42", services.PullRequests.LastRequest.PullRequestBody);
        Assert.Contains("Agent Foreman", services.PullRequests.LastRequest.PullRequestBody);
        Assert.Contains("human review", services.PullRequests.LastRequest.PullRequestBody);
        Assert.Contains("plan.md", services.PullRequests.LastRequest.PullRequestBody);
        Assert.Contains("codex-exec.log", services.PullRequests.LastRequest.PullRequestBody);
    }

    [Fact]
    public void SubmitStillDoesNotMergePr()
    {
        var services = SubmitTestServices.Valid();

        var result = services.Execute(new[] { "submit", "42" });

        Assert.Equal(0, result.ExitCode);
        Assert.DoesNotContain("merge", services.PullRequests.LastRequest!.PullRequestTitle, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SubmitMarksMissionAsPullRequestCreated()
    {
        var services = SubmitTestServices.Valid();

        var result = services.Execute(new[] { "submit", "42" });

        Assert.Equal(0, result.ExitCode);
        Assert.Equal(MissionStatus.PullRequestCreated, services.Missions.Saved.Last().Status);
        Assert.Equal("https://github.com/caio/elevator-ads-mvp/pull/10", services.Missions.Saved.Last().PullRequestUrl);
        Assert.Contains("Pull request created:", result.Output);
    }

    [Fact]
    public void SubmitCallsMarkAsReviewWithPrUrl()
    {
        var services = SubmitTestServices.Valid();

        var result = services.Execute(new[] { "submit", "42" });

        Assert.Equal(0, result.ExitCode);
        Assert.True(services.WorkItems.MarkedAsReview);
    }

    [Fact]
    public void SubmitMarksMissionAsFailedWhenPushFails()
    {
        var gitRepo = new RecordingGitRepository(currentBranch: "agent/issue-42", pushThrows: true);
        var services = SubmitTestServices.Valid(gitRepo: gitRepo);

        var result = services.Execute(new[] { "submit", "42" });

        Assert.NotEqual(0, result.ExitCode);
        Assert.Equal(MissionStatus.Failed, services.Missions.Saved.Last().Status);
        Assert.Contains("Push failed", result.Error);
    }

    [Fact]
    public void SubmitMarksMissionAsFailedWhenPrCreationFails()
    {
        var services = SubmitTestServices.Valid(prSucceeds: false);

        var result = services.Execute(new[] { "submit", "42" });

        Assert.NotEqual(0, result.ExitCode);
        Assert.Equal(MissionStatus.Failed, services.Missions.Saved.Last().Status);
        Assert.Contains("PR creation failed", result.Error);
    }

    [Fact]
    public void SubmitRecordsSubmitStartedAndPullRequestCreatedEvents()
    {
        var services = SubmitTestServices.Valid();

        var result = services.Execute(new[] { "submit", "42" });

        Assert.Equal(0, result.ExitCode);
        Assert.Contains(services.Events.Events, e => e.EventType == MissionEventType.SubmitStarted);
        Assert.Contains(services.Events.Events, e => e.EventType == MissionEventType.PullRequestCreated);
    }
}

internal sealed class SubmitTestServices
{
    private readonly FakeConfigLoader _configLoader;

    private SubmitTestServices(
        FakeConfigLoader configLoader,
        FakeWorkItemProvider workItems,
        FakeMissionRepository missions,
        RecordingGitRepository gitRepo,
        FakePullRequestProvider pullRequests,
        FakeMissionEventRecorder events)
    {
        _configLoader = configLoader;
        WorkItems = workItems;
        Missions = missions;
        GitRepo = gitRepo;
        PullRequests = pullRequests;
        Events = events;
    }

    public FakeWorkItemProvider WorkItems { get; }
    public FakeMissionRepository Missions { get; }
    public RecordingGitRepository GitRepo { get; }
    public FakePullRequestProvider PullRequests { get; }
    public FakeMissionEventRecorder Events { get; }

    public static SubmitTestServices Valid(
        RecordingGitRepository? gitRepo = null,
        bool prSucceeds = true,
        bool workItemExists = true,
        bool seedMission = true,
        MissionStatus missionStatus = MissionStatus.TestsPassed,
        string repoPath = "/workspace/project")
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
            Database = new DatabaseConfig
            {
                Provider = "postgresql",
                ConnectionString = "Host=localhost;Database=agent_foreman",
            },
        };

        var missions = new FakeMissionRepository();
        if (seedMission)
        {
            missions.Save(new Mission(
                "github-42",
                "42",
                "GitHub",
                "Fix elevator ad pacing",
                missionStatus,
                Branch: null,
                PlanPath: null,
                PullRequestUrl: null,
                RetryAfter: null,
                LastError: null,
                CreatedAt: DateTimeOffset.UtcNow,
                UpdatedAt: DateTimeOffset.UtcNow));
        }

        return new SubmitTestServices(
            new FakeConfigLoader(AgentForemanConfigLoadResult.Success(config)),
            new FakeWorkItemProvider(workItemExists),
            missions,
            gitRepo ?? new RecordingGitRepository(currentBranch: "agent/issue-42"),
            new FakePullRequestProvider(prSucceeds),
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
            gitRepository: GitRepo,
            workItemProvider: WorkItems,
            missionRepository: Missions,
            pullRequestProvider: PullRequests,
            missionEventRecorder: Events);
    }
}

internal sealed class RecordingGitRepository : IGitRepository
{
    private readonly bool _pushThrows;
    private readonly bool _pullThrows;

    public RecordingGitRepository(
        string currentBranch = "main",
        bool hasChanges = true,
        bool branchExists = false,
        bool commitCreated = true,
        bool pushThrows = false,
        bool pullThrows = false,
        bool hasOnlyLineEndingChanges = false)
    {
        CurrentBranch = currentBranch;
        Status = hasChanges
            ? new GitStatusResult(new[] { new GitChangedFile(" M", "src/App.cs") })
            : new GitStatusResult(Array.Empty<GitChangedFile>());
        BranchExistsResult = branchExists;
        CommitCreated = commitCreated;
        _pushThrows = pushThrows;
        _pullThrows = pullThrows;
        HasOnlyLineEndingChangesResult = hasOnlyLineEndingChanges;
    }

    public string CurrentBranch { get; }
    public GitStatusResult Status { get; }
    public bool BranchExistsResult { get; }
    public bool CommitCreated { get; }
    public bool HasOnlyLineEndingChangesResult { get; }

    public string? CreatedBranch { get; private set; }
    public string? CheckedOutBranch { get; private set; }
    public List<string> CheckedOutBranches { get; } = new();
    public bool AddAllCalled { get; private set; }
    public string? CommitMessage { get; private set; }
    public string? PushedRemote { get; private set; }
    public string? PushedBranch { get; private set; }

    public Task<bool> IsRepositoryAsync(string repoPath, CancellationToken cancellationToken = default) =>
        Task.FromResult(true);

    public Task<string> GetCurrentBranchAsync(string repoPath, CancellationToken cancellationToken = default) =>
        Task.FromResult(CurrentBranch);

    public Task CheckoutAsync(string repoPath, string branch, CancellationToken cancellationToken = default)
    {
        CheckedOutBranch = branch;
        CheckedOutBranches.Add(branch);
        return Task.CompletedTask;
    }

    public Task PullAsync(string repoPath, string remote, string branch, CancellationToken cancellationToken = default)
    {
        if (_pullThrows)
            throw new InvalidOperationException("pull failed: remote rejected");
        return Task.CompletedTask;
    }

    public Task CreateBranchAsync(string repoPath, string branch, CancellationToken cancellationToken = default)
    {
        CreatedBranch = branch;
        return Task.CompletedTask;
    }

    public Task<bool> BranchExistsAsync(string repoPath, string branch, CancellationToken cancellationToken = default) =>
        Task.FromResult(BranchExistsResult);

    public Task<GitStatusResult> GetStatusAsync(string repoPath, CancellationToken cancellationToken = default) =>
        Task.FromResult(Status);

    public Task<string> GetDiffAsync(string repoPath, CancellationToken cancellationToken = default) =>
        Task.FromResult(string.Empty);

    public Task<bool> HasOnlyLineEndingChangesAsync(string repoPath, CancellationToken cancellationToken = default) =>
        Task.FromResult(HasOnlyLineEndingChangesResult);

    public Task AddAllAsync(string repoPath, CancellationToken cancellationToken = default)
    {
        AddAllCalled = true;
        return Task.CompletedTask;
    }

    public Task<GitCommitResult> CommitAsync(string repoPath, string message, CancellationToken cancellationToken = default)
    {
        CommitMessage = message;
        return Task.FromResult(new GitCommitResult(CommitCreated, string.Empty));
    }

    public Task PushAsync(string repoPath, string remote, string branch, CancellationToken cancellationToken = default)
    {
        if (_pushThrows)
            throw new InvalidOperationException("push failed: remote rejected");
        PushedRemote = remote;
        PushedBranch = branch;
        return Task.CompletedTask;
    }
}

internal sealed class FakePullRequestProvider : IPullRequestProvider
{
    private readonly bool _succeeds;
    private readonly string _prUrl;

    public FakePullRequestProvider(bool succeeds = true, string prUrl = "https://github.com/caio/elevator-ads-mvp/pull/10")
    {
        _succeeds = succeeds;
        _prUrl = prUrl;
    }

    public PullRequestRequest? LastRequest { get; private set; }

    public Task<PullRequestResult> CreateAsync(PullRequestRequest request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        return Task.FromResult(_succeeds
            ? new PullRequestResult(true, _prUrl, _prUrl, string.Empty, 0, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, null)
            : new PullRequestResult(false, null, string.Empty, "pr creation failed", 1, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, "pr creation failed"));
    }
}
