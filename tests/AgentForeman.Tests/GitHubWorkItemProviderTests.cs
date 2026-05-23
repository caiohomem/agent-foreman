using AgentForeman.Core.Commands;
using AgentForeman.Core.Configuration;
using AgentForeman.Core.WorkItems;
using AgentForeman.Infrastructure.WorkItems;

namespace AgentForeman.Tests;

public sealed class GitHubWorkItemProviderTests
{
    [Fact]
    public async Task GitHubIssueListJsonIsParsedCorrectly()
    {
        var runner = new RecordingCommandRunner(GitHubListJson);
        var provider = new GitHubWorkItemProvider(Config(), runner);

        var items = await provider.GetReadyItemsAsync(CancellationToken.None);

        var item = Assert.Single(items);
        Assert.Equal("42", item.ExternalId);
        Assert.Equal(WorkItemSource.GitHub, item.Source);
        Assert.Equal("Fix elevator ad pacing", item.Title);
        Assert.Equal("Issue body", item.Body);
        Assert.Equal("https://github.com/caio/elevator-ads-mvp/issues/42", item.Url);
        Assert.Equal("caio/elevator-ads-mvp", item.Repository);
        Assert.Contains(item.Labels, label => label.Name == "agent-ready");
        Assert.Equal(DateTimeOffset.Parse("2026-05-01T10:00:00Z"), item.CreatedAt);
        Assert.Equal(DateTimeOffset.Parse("2026-05-02T10:00:00Z"), item.UpdatedAt);
        Assert.Equal(WorkItemState.Open, item.State);
    }

    [Fact]
    public async Task GitHubIssueViewJsonIsParsedCorrectly()
    {
        var runner = new RecordingCommandRunner(GitHubViewJson);
        var provider = new GitHubWorkItemProvider(Config(), runner);

        var item = await provider.GetWorkItemAsync("42", CancellationToken.None);

        Assert.Equal("42", item.ExternalId);
        Assert.Equal("Fix elevator ad pacing", item.Title);
        Assert.Contains(item.Labels, label => label.Name == "agent-ready");
        Assert.Equal(WorkItemState.Closed, item.State);
        Assert.Equal(DateTimeOffset.Parse("2026-05-03T10:00:00Z"), item.ClosedAt);
    }

    [Fact]
    public async Task GetReadyItemsAsyncBuildsCorrectGhCommand()
    {
        var runner = new RecordingCommandRunner("[]");
        var provider = new GitHubWorkItemProvider(Config(), runner);

        await provider.GetReadyItemsAsync(CancellationToken.None);

        Assert.Equal("gh", runner.LastRequest!.Executable);
        Assert.Equal(new[]
        {
            "issue", "list", "--repo", "caio/elevator-ads-mvp", "--label", "agent-ready",
            "--json", "number,title,body,url,labels,createdAt,updatedAt,state,closedAt",
        }, runner.LastRequest.Arguments);
    }

    [Fact]
    public async Task GetWorkItemAsyncBuildsCorrectGhCommand()
    {
        var runner = new RecordingCommandRunner(GitHubViewJson);
        var provider = new GitHubWorkItemProvider(Config(), runner);

        await provider.GetWorkItemAsync("42", CancellationToken.None);

        Assert.Equal(new[]
        {
            "issue", "view", "42", "--repo", "caio/elevator-ads-mvp",
            "--json", "number,title,body,url,labels,createdAt,updatedAt,state,closedAt",
        }, runner.LastRequest!.Arguments);
    }

    [Fact]
    public async Task MarkAsCompletedAsyncRemovesReviewAndLifecycleLabels()
    {
        var runner = new RecordingCommandRunner(string.Empty);
        var provider = new GitHubWorkItemProvider(Config(), runner);

        await provider.MarkAsCompletedAsync(Item("42"), CancellationToken.None);

        Assert.Contains(runner.Requests, request => request.Arguments.SequenceEqual(new[] { "issue", "edit", "42", "--repo", "caio/elevator-ads-mvp", "--remove-label", "agent-review" }));
        Assert.Contains(runner.Requests, request => request.Arguments.SequenceEqual(new[] { "issue", "edit", "42", "--repo", "caio/elevator-ads-mvp", "--remove-label", "agent-working" }));
        Assert.Contains(runner.Requests, request => request.Arguments.SequenceEqual(new[] { "issue", "edit", "42", "--repo", "caio/elevator-ads-mvp", "--remove-label", "agent-paused" }));
        Assert.Contains(runner.Requests, request => request.Arguments.SequenceEqual(new[] { "issue", "edit", "42", "--repo", "caio/elevator-ads-mvp", "--remove-label", "agent-blocked" }));
        Assert.Contains(runner.Requests, request => request.Arguments.SequenceEqual(new[] { "issue", "edit", "42", "--repo", "caio/elevator-ads-mvp", "--remove-label", "agent-failed" }));
        Assert.DoesNotContain(runner.Requests, request => request.Arguments.Contains("close"));
    }

    [Fact]
    public async Task AddCommentAsyncBuildsCorrectGhCommand()
    {
        var runner = new RecordingCommandRunner(string.Empty);
        var provider = new GitHubWorkItemProvider(Config(), runner);

        await provider.AddCommentAsync(Item("42"), "hello", CancellationToken.None);

        Assert.Equal(new[] { "issue", "comment", "42", "--repo", "caio/elevator-ads-mvp", "--body", "hello" }, runner.LastRequest!.Arguments);
    }

    [Fact]
    public async Task CreateWorkItemAsyncBuildsCorrectGhCommand()
    {
        var runner = new RecordingCommandRunner("https://github.com/caio/elevator-ads-mvp/issues/99");
        var provider = new GitHubWorkItemProvider(Config(), runner);

        await provider.CreateWorkItemAsync(
            new CreateWorkItemRequest("New issue", "Body", new[] { "agent-ready", "bug" }),
            CancellationToken.None);

        Assert.Equal(new[]
        {
            "issue", "create", "--repo", "caio/elevator-ads-mvp", "--title", "New issue", "--body", "Body",
            "--label", "agent-ready", "--label", "bug",
        }, runner.LastRequest!.Arguments);
    }

    [Fact]
    public async Task MarkAsWorkingAsyncAddsAndRemovesCorrectLabels()
    {
        var runner = new RecordingCommandRunner(string.Empty);
        var provider = new GitHubWorkItemProvider(Config(), runner);

        await provider.MarkAsWorkingAsync(Item("42"), CancellationToken.None);

        Assert.Contains(runner.Requests, request => request.Arguments.SequenceEqual(new[] { "issue", "edit", "42", "--repo", "caio/elevator-ads-mvp", "--add-label", "agent-working" }));
        Assert.Contains(runner.Requests, request => request.Arguments.SequenceEqual(new[] { "issue", "edit", "42", "--repo", "caio/elevator-ads-mvp", "--remove-label", "agent-paused" }));
        Assert.Contains(runner.Requests, request => request.Arguments.SequenceEqual(new[] { "issue", "edit", "42", "--repo", "caio/elevator-ads-mvp", "--remove-label", "agent-ready" }));
        Assert.Contains(runner.Requests, request => request.Arguments.SequenceEqual(new[] { "issue", "edit", "42", "--repo", "caio/elevator-ads-mvp", "--remove-label", "agent-blocked" }));
        Assert.Contains(runner.Requests, request => request.Arguments.SequenceEqual(new[] { "issue", "edit", "42", "--repo", "caio/elevator-ads-mvp", "--remove-label", "agent-failed" }));
    }

    [Fact]
    public async Task MarkAsPausedAsyncAddsLabelAndComments()
    {
        var runner = new RecordingCommandRunner(string.Empty);
        var provider = new GitHubWorkItemProvider(Config(), runner);
        var retryAfter = DateTimeOffset.Parse("2026-05-21T12:00:00Z");

        await provider.MarkAsPausedAsync(Item("42"), "quota reached", retryAfter, CancellationToken.None);

        Assert.Contains(runner.Requests, request => request.Arguments.SequenceEqual(new[] { "issue", "edit", "42", "--repo", "caio/elevator-ads-mvp", "--add-label", "agent-paused" }));
        Assert.Contains(runner.Requests, request => request.Arguments.SequenceEqual(new[] { "issue", "edit", "42", "--repo", "caio/elevator-ads-mvp", "--remove-label", "agent-working" }));
        Assert.Contains(runner.Requests, request => request.Arguments.SequenceEqual(new[] { "issue", "edit", "42", "--repo", "caio/elevator-ads-mvp", "--remove-label", "agent-ready" }));
        Assert.Contains(runner.Requests, request =>
            request.Arguments.Take(5).SequenceEqual(new[] { "issue", "comment", "42", "--repo", "caio/elevator-ads-mvp" })
            && request.Arguments.Any(argument => argument.Contains("quota reached", StringComparison.Ordinal)));
    }

    [Fact]
    public async Task MarkAsReviewAsyncAddsAndRemovesLabelsAndComments()
    {
        var runner = new RecordingCommandRunner(string.Empty);
        var provider = new GitHubWorkItemProvider(Config(), runner);

        await provider.MarkAsReviewAsync(Item("42"), "https://github.com/caio/elevator-ads-mvp/pull/5", CancellationToken.None);

        Assert.Contains(runner.Requests, request => request.Arguments.SequenceEqual(new[] { "issue", "edit", "42", "--repo", "caio/elevator-ads-mvp", "--add-label", "agent-review" }));
        Assert.Contains(runner.Requests, request => request.Arguments.SequenceEqual(new[] { "issue", "edit", "42", "--repo", "caio/elevator-ads-mvp", "--remove-label", "agent-working" }));
        Assert.Contains(runner.Requests, request => request.Arguments.SequenceEqual(new[] { "issue", "edit", "42", "--repo", "caio/elevator-ads-mvp", "--remove-label", "agent-ready" }));
        Assert.Contains(runner.Requests, request => request.Arguments.SequenceEqual(new[] { "issue", "edit", "42", "--repo", "caio/elevator-ads-mvp", "--remove-label", "agent-paused" }));
        Assert.Contains(runner.Requests, request => request.Arguments.SequenceEqual(new[] { "issue", "edit", "42", "--repo", "caio/elevator-ads-mvp", "--remove-label", "agent-blocked" }));
        Assert.Contains(runner.Requests, request => request.Arguments.SequenceEqual(new[] { "issue", "edit", "42", "--repo", "caio/elevator-ads-mvp", "--remove-label", "agent-failed" }));
        Assert.Contains(runner.Requests, request =>
            request.Arguments.Take(5).SequenceEqual(new[] { "issue", "comment", "42", "--repo", "caio/elevator-ads-mvp" })
            && request.Arguments.Any(argument => argument.Contains("https://github.com/caio/elevator-ads-mvp/pull/5", StringComparison.Ordinal)));
    }

    [Fact]
    public async Task MarkAsBlockedAsyncAddsBlockedLabelWithoutMarkingWorking()
    {
        var runner = new RecordingCommandRunner(string.Empty);
        var provider = new GitHubWorkItemProvider(Config(), runner);

        await provider.MarkAsBlockedAsync(
            Item("42"),
            new[] { new WorkItemDependency("1", "caio/elevator-ads-mvp", WorkItemDependencyStatus.Unsatisfied) },
            CancellationToken.None);

        Assert.Contains(runner.Requests, request => request.Arguments.SequenceEqual(new[] { "issue", "edit", "42", "--repo", "caio/elevator-ads-mvp", "--add-label", "agent-blocked" }));
        Assert.DoesNotContain(runner.Requests, request => request.Arguments.SequenceEqual(new[] { "issue", "edit", "42", "--repo", "caio/elevator-ads-mvp", "--add-label", "agent-working" }));
    }

    [Fact]
    public async Task MarkAsFailedAsyncAddsFailedLabelAndRemovesLifecycleLabels()
    {
        var runner = new RecordingCommandRunner(string.Empty);
        var provider = new GitHubWorkItemProvider(Config(), runner);

        await provider.MarkAsFailedAsync(Item("42"), "tests failed", CancellationToken.None);

        Assert.Contains(runner.Requests, request => request.Arguments.SequenceEqual(new[] { "issue", "edit", "42", "--repo", "caio/elevator-ads-mvp", "--add-label", "agent-failed" }));
        Assert.Contains(runner.Requests, request => request.Arguments.SequenceEqual(new[] { "issue", "edit", "42", "--repo", "caio/elevator-ads-mvp", "--remove-label", "agent-working" }));
        Assert.Contains(runner.Requests, request => request.Arguments.SequenceEqual(new[] { "issue", "edit", "42", "--repo", "caio/elevator-ads-mvp", "--remove-label", "agent-ready" }));
        Assert.Contains(runner.Requests, request => request.Arguments.SequenceEqual(new[] { "issue", "edit", "42", "--repo", "caio/elevator-ads-mvp", "--remove-label", "agent-paused" }));
    }

    private static AgentForemanConfig Config()
    {
        return new AgentForemanConfig
        {
            WorkItems = new WorkItemsConfig
            {
                Provider = "github",
                Repo = "caio/elevator-ads-mvp",
                ReadyLabel = "agent-ready",
                WorkingLabel = "agent-working",
                ReviewLabel = "agent-review",
                PausedLabel = "agent-paused",
                BlockedLabel = "agent-blocked",
                FailedLabel = "agent-failed",
            },
        };
    }

    private static WorkItem Item(string id)
    {
        return new WorkItem(
            id,
            WorkItemSource.GitHub,
            "Title",
            "Body",
            "https://github.com/caio/elevator-ads-mvp/issues/" + id,
            "caio/elevator-ads-mvp",
            Array.Empty<WorkItemLabel>(),
            DateTimeOffset.Parse("2026-05-01T10:00:00Z"),
            DateTimeOffset.Parse("2026-05-02T10:00:00Z"));
    }

    private const string GitHubListJson = """
        [
          {
            "number": 42,
            "title": "Fix elevator ad pacing",
            "body": "Issue body",
            "url": "https://github.com/caio/elevator-ads-mvp/issues/42",
            "labels": [{"name": "agent-ready"}],
            "createdAt": "2026-05-01T10:00:00Z",
            "updatedAt": "2026-05-02T10:00:00Z",
            "state": "OPEN",
            "closedAt": null
          }
        ]
        """;

    private const string GitHubViewJson = """
        {
          "number": 42,
          "title": "Fix elevator ad pacing",
          "body": "Issue body",
          "url": "https://github.com/caio/elevator-ads-mvp/issues/42",
          "labels": [{"name": "agent-ready"}],
          "createdAt": "2026-05-01T10:00:00Z",
          "updatedAt": "2026-05-02T10:00:00Z",
          "state": "CLOSED",
          "closedAt": "2026-05-03T10:00:00Z"
        }
        """;
}

internal sealed class RecordingCommandRunner : ICommandRunner
{
    private readonly Queue<string> _stdoutResponses;
    private readonly Queue<AgentForeman.Core.Commands.CommandResult>? _results;

    public RecordingCommandRunner(params string[] stdoutResponses)
    {
        _stdoutResponses = new Queue<string>(stdoutResponses.Length == 0 ? new[] { string.Empty } : stdoutResponses);
    }

    private RecordingCommandRunner(params AgentForeman.Core.Commands.CommandResult[] results)
    {
        _stdoutResponses = new Queue<string>();
        _results = new Queue<AgentForeman.Core.Commands.CommandResult>(results);
    }

    public static RecordingCommandRunner FromResults(params AgentForeman.Core.Commands.CommandResult[] results)
    {
        return new RecordingCommandRunner(results);
    }

    public List<CommandRequest> Requests { get; } = new();
    public CommandRequest? LastRequest => Requests.LastOrDefault();

    public Task<AgentForeman.Core.Commands.CommandResult> RunAsync(
        CommandRequest request,
        Action<CommandOutputLine>? onOutputLine = null,
        CancellationToken cancellationToken = default)
    {
        Requests.Add(request);
        if (_results is not null)
        {
            return Task.FromResult(_results.Count == 0
                ? new AgentForeman.Core.Commands.CommandResult(0, string.Empty, string.Empty, string.Empty, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow)
                : _results.Dequeue());
        }

        var stdout = _stdoutResponses.Count == 0 ? string.Empty : _stdoutResponses.Dequeue();
        return Task.FromResult(new AgentForeman.Core.Commands.CommandResult(
            0,
            stdout,
            string.Empty,
            stdout,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow));
    }
}
