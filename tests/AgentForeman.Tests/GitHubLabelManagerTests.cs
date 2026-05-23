using AgentForeman.Core.Configuration;
using AgentForeman.Core.Labels;
using AgentForeman.Infrastructure.Labels;

namespace AgentForeman.Tests;

public sealed class GitHubLabelManagerTests
{
    [Fact]
    public async Task LabelsSyncCreatesMissingLabels()
    {
        var runner = new RecordingCommandRunner("[]", "", "", "", "", "", "");
        var manager = new GitHubLabelManager(runner);

        var result = await manager.SyncAsync(Config(), CancellationToken.None);

        Assert.Equal(6, result.Results.Count(resultItem => resultItem.Created));
        Assert.Contains(runner.Requests, request => request.Arguments.SequenceEqual(new[]
        {
            "label", "create", "agent-ready", "--repo", "caiohomem/elevator-ads-mvp",
            "--color", "0e8a16", "--description", "Ready for Agent Foreman to process."
        }));
    }

    [Fact]
    public async Task LabelsSyncDoesNotRecreateExistingLabels()
    {
        var runner = new RecordingCommandRunner(
            """
            [{"name":"agent-ready"},{"name":"agent-working"},{"name":"agent-review"},{"name":"agent-paused"},{"name":"agent-blocked"},{"name":"agent-failed"}]
            """);
        var manager = new GitHubLabelManager(runner);

        var result = await manager.SyncAsync(Config(), CancellationToken.None);

        Assert.Equal(6, result.Results.Count(resultItem => resultItem.Existed));
        Assert.DoesNotContain(runner.Requests, request => request.Arguments.Take(2).SequenceEqual(new[] { "label", "create" }));
    }

    [Fact]
    public async Task LabelsSyncUsesConfiguredLabelNames()
    {
        var runner = new RecordingCommandRunner("[]", "", "", "", "", "", "");
        var manager = new GitHubLabelManager(runner);
        var config = new AgentForemanConfig
        {
            WorkItems = new WorkItemsConfig
            {
                Provider = "github",
                Repo = "caiohomem/elevator-ads-mvp",
                ReadyLabel = "custom-ready",
                WorkingLabel = "custom-working",
                ReviewLabel = "custom-review",
                PausedLabel = "custom-paused",
                BlockedLabel = "custom-blocked",
                FailedLabel = "custom-failed",
            },
        };

        await manager.SyncAsync(config, CancellationToken.None);

        Assert.Contains(runner.Requests, request => request.Arguments.Contains("custom-ready"));
        Assert.Contains(runner.Requests, request => request.Arguments.Contains("custom-failed"));
    }

    [Fact]
    public async Task LabelsSyncUsesDefaultsForBlockedAndFailedLabels()
    {
        var runner = new RecordingCommandRunner("[]", "", "", "", "", "", "");
        var manager = new GitHubLabelManager(runner);
        var config = new AgentForemanConfig
        {
            WorkItems = new WorkItemsConfig
            {
                Provider = "github",
                Repo = "caiohomem/elevator-ads-mvp",
                ReadyLabel = "agent-ready",
                WorkingLabel = "agent-working",
                ReviewLabel = "agent-review",
                PausedLabel = "agent-paused",
                BlockedLabel = "",
                FailedLabel = "",
            },
        };

        await manager.SyncAsync(config, CancellationToken.None);

        Assert.Contains(runner.Requests, request => request.Arguments.Contains("agent-blocked"));
        Assert.Contains(runner.Requests, request => request.Arguments.Contains("agent-failed"));
    }

    [Fact]
    public async Task LabelsSyncReturnsFailureWhenGhFails()
    {
        var runner = RecordingCommandRunner.FromResults(
            CreateCoreResult(1, "", "gh failed"));
        var manager = new GitHubLabelManager(runner);

        await Assert.ThrowsAsync<InvalidOperationException>(() => manager.SyncAsync(Config(), CancellationToken.None));
    }

    [Fact]
    public async Task LabelsListBuildsCorrectGhCommand()
    {
        var runner = new RecordingCommandRunner("""[{"name":"agent-ready"}]""");
        var manager = new GitHubLabelManager(runner);

        var labels = await manager.ListAsync("caiohomem/elevator-ads-mvp", CancellationToken.None);

        Assert.Single(labels);
        Assert.Equal(new[] { "label", "list", "--repo", "caiohomem/elevator-ads-mvp", "--json", "name" }, runner.LastRequest!.Arguments);
    }

    private static AgentForemanConfig Config()
    {
        return new AgentForemanConfig
        {
            WorkItems = new WorkItemsConfig
            {
                Provider = "github",
                Repo = "caiohomem/elevator-ads-mvp",
                ReadyLabel = "agent-ready",
                WorkingLabel = "agent-working",
                ReviewLabel = "agent-review",
                PausedLabel = "agent-paused",
                BlockedLabel = "agent-blocked",
                FailedLabel = "agent-failed",
            },
        };
    }

    private static AgentForeman.Core.Commands.CommandResult CreateCoreResult(int exitCode, string stdout, string stderr)
    {
        return new AgentForeman.Core.Commands.CommandResult(
            exitCode,
            stdout,
            stderr,
            string.Concat(stdout, stderr),
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);
    }
}
