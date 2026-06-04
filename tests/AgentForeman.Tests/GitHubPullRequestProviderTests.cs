using AgentForeman.Core.Commands;
using AgentForeman.Core.PullRequests;
using AgentForeman.Infrastructure.PullRequests;

namespace AgentForeman.Tests;

public sealed class GitHubPullRequestProviderTests
{
    [Fact]
    public async Task EnableAutoMergeAsyncInvokesGhPrMergeAutoSquashByDefault()
    {
        var runner = new RecordingCommandRunner("https://github.com/caio/repo/pull/77");
        var provider = new GitHubPullRequestProvider(runner);

        var result = await provider.EnableAutoMergeAsync(
            new PullRequestAutoMergeRequest("caio/repo", "https://github.com/caio/repo/pull/77", "squash"),
            CancellationToken.None);

        Assert.True(result.Success);
        Assert.NotNull(runner.LastRequest);
        Assert.Equal("gh", runner.LastRequest!.Executable);
        Assert.Equal(new[] { "pr", "merge", "77", "--repo", "caio/repo", "--auto", "--squash" },
            runner.LastRequest.Arguments);
    }

    [Fact]
    public async Task EnableAutoMergeAsyncMapsMethodArgumentToFlag()
    {
        var runner = new RecordingCommandRunner("https://github.com/caio/repo/pull/78");
        var provider = new GitHubPullRequestProvider(runner);

        await provider.EnableAutoMergeAsync(
            new PullRequestAutoMergeRequest("caio/repo", "https://github.com/caio/repo/pull/78", "rebase"),
            CancellationToken.None);

        Assert.NotNull(runner.LastRequest);
        Assert.Equal(new[] { "pr", "merge", "78", "--repo", "caio/repo", "--auto", "--rebase" },
            runner.LastRequest!.Arguments);
    }

    [Fact]
    public async Task EnableAutoMergeAsyncReturnsFailureOnNonZeroExit()
    {
        var runner = RecordingCommandRunner.FromResults(new CommandResult(
            ExitCode: 1,
            StdoutText: string.Empty,
            StderrText: "Cannot enable auto-merge on this PR.",
            CombinedOutputText: string.Empty,
            StartedAt: DateTimeOffset.UtcNow,
            FinishedAt: DateTimeOffset.UtcNow));
        var provider = new GitHubPullRequestProvider(runner);

        var result = await provider.EnableAutoMergeAsync(
            new PullRequestAutoMergeRequest("caio/repo", "https://github.com/caio/repo/pull/79", "squash"),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("Cannot enable auto-merge on this PR.", result.ErrorMessage);
    }
}
