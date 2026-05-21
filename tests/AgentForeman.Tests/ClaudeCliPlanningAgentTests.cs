using AgentForeman.Core.Commands;
using AgentForeman.Core.Configuration;
using AgentForeman.Core.Planning;
using AgentForeman.Core.WorkItems;
using AgentForeman.Infrastructure.Planning;

namespace AgentForeman.Tests;

public sealed class ClaudeCliPlanningAgentTests
{
    [Fact]
    public async Task ClaudePlannerBuildsExpectedCommand()
    {
        using var workspace = TemporaryDirectory.Create();
        var runner = new RecordingCommandRunner("# Plan");
        var agent = new ClaudeCliPlanningAgent(runner);
        var request = Request(workspace.Path);

        await agent.CreatePlanAsync(request, CancellationToken.None);

        Assert.Equal("claude", runner.LastRequest!.Executable);
        Assert.Equal(workspace.Path, runner.LastRequest.WorkingDirectory);
        Assert.Equal("--print", runner.LastRequest.Arguments[0]);
        Assert.Contains("Create a technical implementation plan", runner.LastRequest.Arguments[1]);
        Assert.Contains("Do not modify files.", runner.LastRequest.Arguments[1]);
        Assert.Contains("Issue #42: Fix elevator ad pacing", runner.LastRequest.Arguments[1]);
    }

    [Fact]
    public async Task PlanAndLogPathsAreGeneratedCorrectly()
    {
        using var workspace = TemporaryDirectory.Create();
        var agent = new ClaudeCliPlanningAgent(new RecordingCommandRunner("# Plan"));
        var request = Request(workspace.Path);

        var result = await agent.CreatePlanAsync(request, CancellationToken.None);

        Assert.Equal(Path.Combine(workspace.Path, ".agent", "runs", "issue-42", "plan.md"), result.PlanPath);
        Assert.Equal(Path.Combine(workspace.Path, ".agent", "runs", "issue-42", "claude-plan.log"), result.LogPath);
    }

    [Fact]
    public async Task OutputDirectoriesAreCreated()
    {
        using var workspace = TemporaryDirectory.Create();
        var agent = new ClaudeCliPlanningAgent(new RecordingCommandRunner("# Plan"));
        var request = Request(workspace.Path);

        var result = await agent.CreatePlanAsync(request, CancellationToken.None);

        Assert.True(Directory.Exists(request.OutputDirectory));
        Assert.True(File.Exists(result.PlanPath));
        Assert.True(File.Exists(result.LogPath));
    }

    private static PlanningRequest Request(string repoPath)
    {
        return new PlanningRequest(
            WorkItemId: "42",
            Title: "Fix elevator ad pacing",
            Body: "Issue body",
            Labels: new[] { new WorkItemLabel("agent-ready") },
            Repository: "caio/elevator-ads-mvp",
            RepoPath: repoPath,
            OutputDirectory: Path.Combine(repoPath, ".agent", "runs", "issue-42"),
            AgentsContent: "Agent rules",
            Config: new AgentForemanConfig
            {
                Planner = new PlannerConfig
                {
                    Provider = "claude-cli",
                    Command = "claude",
                },
            });
    }
}

