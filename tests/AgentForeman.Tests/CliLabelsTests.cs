using AgentForeman.Cli;
using AgentForeman.Core.Configuration;
using AgentForeman.Core.Labels;

namespace AgentForeman.Tests;

public sealed class CliLabelsTests
{
    [Fact]
    public void LabelsListPrintsExistingLabels()
    {
        var services = DoctorTestServices.Valid();
        var manager = new FakeLabelManager(
            labels: new[] { "agent-ready", "agent-working" },
            syncResults: Array.Empty<LabelSyncItemResult>());

        var result = CliApplication.Execute(
            new[] { "labels", "list" },
            services.ConfigLoader,
            services.RepositoryChecker,
            services.CommandChecker,
            new FakeStateStore(),
            new RecordingCommandRunner(),
            labelManager: manager);

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("agent-ready", result.Output);
        Assert.Contains("agent-working", result.Output);
    }

    [Fact]
    public void LabelsSyncUsesConfiguredLabelNames()
    {
        var services = DoctorTestServices.Valid();
        var manager = new FakeLabelManager(
            labels: Array.Empty<string>(),
            syncResults: new[]
            {
                new LabelSyncItemResult("custom-ready", true, false),
                new LabelSyncItemResult("custom-failed", false, true),
            });
        var config = new AgentForemanConfig
        {
            Project = new ProjectConfig { Name = "project", RepoPath = "/workspace/project", DefaultBranch = "main" },
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
            Planner = new PlannerConfig { Provider = "claude-cli", Command = "claude" },
            Executor = new ExecutorConfig { Provider = "codex-cli", Command = "codex" },
            Database = new DatabaseConfig { Provider = "postgresql", ConnectionString = "Host=localhost;Database=agent_foreman" },
        };

        var result = CliApplication.Execute(
            new[] { "labels", "sync" },
            new FakeConfigLoader(AgentForemanConfigLoadResult.Success(config)),
            services.RepositoryChecker,
            services.CommandChecker,
            new FakeStateStore(),
            new RecordingCommandRunner(),
            labelManager: manager);

        Assert.Equal(0, result.ExitCode);
        Assert.Equal("custom-ready", manager.LastConfig!.WorkItems.ReadyLabel);
        Assert.Equal("custom-failed", manager.LastConfig.WorkItems.FailedLabel);
    }

    [Fact]
    public void LabelsSyncReturnsNonZeroWhenGhFails()
    {
        var services = DoctorTestServices.Valid();
        var manager = new FakeLabelManager(
            labels: Array.Empty<string>(),
            syncResults: Array.Empty<LabelSyncItemResult>(),
            syncException: new InvalidOperationException("gh failed"));

        var result = CliApplication.Execute(
            new[] { "labels", "sync" },
            services.ConfigLoader,
            services.RepositoryChecker,
            services.CommandChecker,
            new FakeStateStore(),
            new RecordingCommandRunner(),
            labelManager: manager);

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("gh failed", result.Error);
    }
}

internal sealed class FakeLabelManager : ILabelManager
{
    private readonly IReadOnlyList<string> _labels;
    private readonly IReadOnlyList<LabelSyncItemResult> _syncResults;
    private readonly Exception? _syncException;

    public FakeLabelManager(IReadOnlyList<string> labels, IReadOnlyList<LabelSyncItemResult> syncResults, Exception? syncException = null)
    {
        _labels = labels;
        _syncResults = syncResults;
        _syncException = syncException;
    }

    public AgentForemanConfig? LastConfig { get; private set; }

    public Task<IReadOnlyList<string>> ListAsync(string repository, CancellationToken cancellationToken)
    {
        return Task.FromResult(_labels);
    }

    public Task<LabelSyncResult> SyncAsync(AgentForemanConfig config, CancellationToken cancellationToken)
    {
        LastConfig = config;
        if (_syncException is not null)
        {
            throw _syncException;
        }

        return Task.FromResult(new LabelSyncResult(_syncResults));
    }
}
