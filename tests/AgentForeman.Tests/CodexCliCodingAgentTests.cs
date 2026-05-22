using AgentForeman.Core.Coding;
using AgentForeman.Core.Configuration;
using AgentForeman.Core.WorkItems;
using AgentForeman.Infrastructure.Coding;

namespace AgentForeman.Tests;

public sealed class CodexCliCodingAgentTests
{
    [Fact]
    public async Task CodexExecutorBuildsExpectedCommand()
    {
        using var workspace = TemporaryDirectory.Create();
        var runner = new RecordingCommandRunner(string.Empty);
        var agent = new CodexCliCodingAgent(runner);
        var request = Request(workspace.Path);

        await agent.ExecuteAsync(request, CancellationToken.None);

        Assert.Equal("codex", runner.LastRequest!.Executable);
        Assert.Equal(workspace.Path, runner.LastRequest.WorkingDirectory);
        var args = runner.LastRequest.Arguments;
        Assert.Equal("--ask-for-approval", args[0]);
        Assert.Equal("never", args[1]);
        Assert.Equal("exec", args[2]);
        Assert.Equal("--sandbox", args[3]);
        Assert.Equal("workspace-write", args[4]);
        Assert.Equal("--cd", args[5]);
        Assert.Equal(workspace.Path, args[6]);
        var prompt = args[7];
        Assert.Contains("Implement the work item", prompt);
        Assert.Contains("Do not commit changes.", prompt);
        Assert.Contains("Do not push.", prompt);
        Assert.Contains("Do not create pull requests.", prompt);
        Assert.Contains("Issue #42: Fix elevator ad pacing", prompt);
        Assert.Contains("Plan:", prompt);
        Assert.Contains("# Saved plan", prompt);
    }

    [Fact]
    public async Task CodexLogPathIsGeneratedCorrectly()
    {
        using var workspace = TemporaryDirectory.Create();
        var agent = new CodexCliCodingAgent(new RecordingCommandRunner(string.Empty));
        var request = Request(workspace.Path);

        var result = await agent.ExecuteAsync(request, CancellationToken.None);

        Assert.Equal(Path.Combine(workspace.Path, ".agent", "runs", "issue-42", "codex-exec.log"), result.LogPath);
    }

    [Fact]
    public async Task CodexOutputDirectoriesAreCreated()
    {
        using var workspace = TemporaryDirectory.Create();
        var agent = new CodexCliCodingAgent(new RecordingCommandRunner(string.Empty));
        var request = Request(workspace.Path);

        var result = await agent.ExecuteAsync(request, CancellationToken.None);

        Assert.True(Directory.Exists(request.OutputDirectory));
        Assert.True(File.Exists(result.LogPath));
    }

    private static CodingRequest Request(string repoPath)
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
                    Provider = "codex-cli",
                    Command = "codex",
                    Sandbox = "workspace-write",
                    Approval = "never",
                },
            });
    }
}
