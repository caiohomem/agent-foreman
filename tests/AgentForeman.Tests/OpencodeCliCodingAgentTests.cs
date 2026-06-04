using AgentForeman.Core.Coding;
using AgentForeman.Core.Configuration;
using AgentForeman.Core.WorkItems;
using AgentForeman.Infrastructure.Coding;

namespace AgentForeman.Tests;

public sealed class OpencodeCliCodingAgentTests
{
    [Fact]
    public async Task OpencodeExecutorBuildsExpectedCommand()
    {
        using var workspace = TemporaryDirectory.Create();
        var runner = new RecordingCommandRunner(string.Empty);
        var agent = new OpencodeCliCodingAgent(runner);
        var request = Request(workspace.Path);

        await agent.ExecuteAsync(request, CancellationToken.None);

        Assert.Equal("opencode", runner.LastRequest!.Executable);
        Assert.Equal(workspace.Path, runner.LastRequest.WorkingDirectory);
        var args = runner.LastRequest.Arguments;
        Assert.Equal("run", args[0]);
        Assert.Equal("--model", args[1]);
        Assert.Equal("opencode/minimax-m3-free", args[2]);
        Assert.Equal("--dir", args[3]);
        Assert.Equal(workspace.Path, args[4]);
        Assert.Equal("--dangerously-skip-permissions", args[5]);
        var prompt = args[6];
        Assert.Contains("Implement the work item", prompt);
        Assert.Contains("Do not commit changes.", prompt);
        Assert.Contains("Do not push.", prompt);
        Assert.Contains("Do not create pull requests.", prompt);
        Assert.Contains("Issue #42: Fix elevator ad pacing", prompt);
        Assert.Contains("Plan:", prompt);
        Assert.Contains("# Saved plan", prompt);
    }

    [Fact]
    public async Task OpencodeUsesConfiguredModelWhenProvided()
    {
        using var workspace = TemporaryDirectory.Create();
        var runner = new RecordingCommandRunner(string.Empty);
        var agent = new OpencodeCliCodingAgent(runner);
        var request = Request(workspace.Path, model: "opencode/another-model");

        await agent.ExecuteAsync(request, CancellationToken.None);

        var args = runner.LastRequest!.Arguments;
        Assert.Equal("--model", args[1]);
        Assert.Equal("opencode/another-model", args[2]);
    }

    [Fact]
    public async Task OpencodeLogPathIsGeneratedCorrectly()
    {
        using var workspace = TemporaryDirectory.Create();
        var agent = new OpencodeCliCodingAgent(new RecordingCommandRunner(string.Empty));
        var request = Request(workspace.Path);

        var result = await agent.ExecuteAsync(request, CancellationToken.None);

        Assert.Equal(Path.Combine(workspace.Path, ".agent", "runs", "issue-42", "opencode-exec.log"), result.LogPath);
    }

    [Fact]
    public async Task OpencodeOutputDirectoriesAreCreated()
    {
        using var workspace = TemporaryDirectory.Create();
        var agent = new OpencodeCliCodingAgent(new RecordingCommandRunner(string.Empty));
        var request = Request(workspace.Path);

        var result = await agent.ExecuteAsync(request, CancellationToken.None);

        Assert.True(Directory.Exists(request.OutputDirectory));
        Assert.True(File.Exists(result.LogPath));
    }

    [Fact]
    public void FactorySelectsOpencodeAgentForOpencodeCliProvider()
    {
        var config = new AgentForemanConfig
        {
            Executor = new ExecutorConfig { Provider = "opencode-cli", Command = "opencode" },
        };

        var agent = CodingAgentFactory.Create(config, new RecordingCommandRunner());

        Assert.IsType<OpencodeCliCodingAgent>(agent);
    }

    [Fact]
    public void FactorySelectsCodexAgentForCodexCliProvider()
    {
        var config = new AgentForemanConfig
        {
            Executor = new ExecutorConfig { Provider = "codex-cli", Command = "codex" },
        };

        var agent = CodingAgentFactory.Create(config, new RecordingCommandRunner());

        Assert.IsType<CodexCliCodingAgent>(agent);
    }

    [Fact]
    public void FactoryThrowsForUnknownProvider()
    {
        var config = new AgentForemanConfig
        {
            Executor = new ExecutorConfig { Provider = "unknown-cli", Command = "x" },
        };

        Assert.Throws<InvalidOperationException>(() =>
            CodingAgentFactory.Create(config, new RecordingCommandRunner()));
    }

    private static CodingRequest Request(string repoPath, string? model = null)
    {
        return new CodingRequest(
            WorkItemId: "42",
            Title: "Fix elevator ad pacing",
            Body: "Issue body",
            Labels: new[] { new WorkItemLabel("agent-ready") },
            Repository: "caio/elevator-ads-mvp",
            RepoPath: repoPath,
            PlanPath: Path.Combine(repoPath, ".agent", "runs", "issue-42", "plan.md"),
            PlanContent: "# Saved plan",
            OutputDirectory: Path.Combine(repoPath, ".agent", "runs", "issue-42"),
            AgentsContent: "Agent rules",
            PreviousLogs: null,
            CurrentDiff: null,
            Config: new AgentForemanConfig
            {
                Executor = new ExecutorConfig
                {
                    Provider = "opencode-cli",
                    Command = "opencode",
                    Model = model ?? string.Empty,
                },
            });
    }
}
