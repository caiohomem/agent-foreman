using AgentForeman.Core.Recovery;
using AgentForeman.Infrastructure.Commands;
using AgentForeman.Infrastructure.Git;
using AgentForeman.Infrastructure.Recovery;

namespace AgentForeman.Tests;

public sealed class RecoveryAgentTests
{
    [Fact]
    public void ParsesStructuredDiagnosis()
    {
        var diagnosis = ClaudeCliRecoveryAgent.Parse(
            """{"category":"DirtyWorktree","diagnosis":"dirty","proposedAction":"stash","lessonTitle":"Clean branches","lessonBody":"stash first","confidence":0.9}""");
        Assert.Equal(RecoveryCategory.DirtyWorktree, diagnosis.Category);
        Assert.Equal("stash", diagnosis.ProposedAction);
    }

    [Fact]
    public void InvalidJsonFallsBackToNeedsHuman()
    {
        Assert.Equal(RecoveryCategory.NeedsHuman, ClaudeCliRecoveryAgent.Parse("not json").Category);
    }

    [Fact]
    public async Task DirtyWorktreeRemediationCreatesNamedStash()
    {
        using var repo = await TemporaryGitRepository.CreateAsync();
        if (repo is null) return;
        await File.WriteAllTextAsync(Path.Combine(repo.Path, "untracked.txt"), "change");
        var runner = new ProcessCommandRunner();
        var remediator = new RecoveryRemediator(new CliGitRepository(runner));

        var result = await remediator.RemediateAsync(
            new RecoveryDiagnosis(RecoveryCategory.DirtyWorktree, "dirty", "stash", "title", "body", 1),
            new RemediationContext("github-42", repo.Path, FailedStage.BranchPrep), CancellationToken.None);

        Assert.True(result.Success);
        var stashList = await runner.RunAsync(new AgentForeman.Core.Commands.CommandRequest(
            "git", new[] { "stash", "list" }, WorkingDirectory: repo.Path));
        Assert.Contains("agent-foreman/recovery-github-42-", stashList.StdoutText);
    }
}
