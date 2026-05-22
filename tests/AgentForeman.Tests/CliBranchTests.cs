using AgentForeman.Cli;
using AgentForeman.Core.Coding;
using AgentForeman.Core.Configuration;
using AgentForeman.Core.Git;
using AgentForeman.Core.Planning;
using AgentForeman.Core.State;
using AgentForeman.Infrastructure.Git;

namespace AgentForeman.Tests;

public sealed class CliBranchTests
{
    // --- MissionBranchPreparer unit tests ---

    [Fact]
    public void BranchPreparer_AlreadyOnMissionBranch_ReturnsOkWithoutSwitch()
    {
        var gitRepo = new RecordingGitRepository(currentBranch: "agent/issue-42");
        var preparer = new MissionBranchPreparer(gitRepo);

        var result = preparer.PrepareAsync(
            new BranchPreparationRequest("/workspace/project", "main", "agent/issue-42"),
            CancellationToken.None).GetAwaiter().GetResult();

        Assert.True(result.Success);
        Assert.Empty(gitRepo.CheckedOutBranches);
        Assert.Null(gitRepo.CreatedBranch);
    }

    [Fact]
    public void BranchPreparer_DirtyWorkdir_ReturnsFail()
    {
        var gitRepo = new RecordingGitRepository(currentBranch: "main", hasChanges: true);
        var preparer = new MissionBranchPreparer(gitRepo);

        var result = preparer.PrepareAsync(
            new BranchPreparationRequest("/workspace/project", "main", "agent/issue-42"),
            CancellationToken.None).GetAwaiter().GetResult();

        Assert.False(result.Success);
        Assert.Contains("uncommitted", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BranchPreparer_CleanWorkdir_NonExistentBranch_CreatesMissionBranch()
    {
        var gitRepo = new RecordingGitRepository(currentBranch: "main", hasChanges: false, branchExists: false);
        var preparer = new MissionBranchPreparer(gitRepo);

        var result = preparer.PrepareAsync(
            new BranchPreparationRequest("/workspace/project", "main", "agent/issue-42"),
            CancellationToken.None).GetAwaiter().GetResult();

        Assert.True(result.Success);
        Assert.Equal("agent/issue-42", gitRepo.CreatedBranch);
        Assert.Contains("main", gitRepo.CheckedOutBranches);
    }

    [Fact]
    public void BranchPreparer_CleanWorkdir_ExistingBranch_ChecksOutWithoutCreating()
    {
        var gitRepo = new RecordingGitRepository(currentBranch: "main", hasChanges: false, branchExists: true);
        var preparer = new MissionBranchPreparer(gitRepo);

        var result = preparer.PrepareAsync(
            new BranchPreparationRequest("/workspace/project", "main", "agent/issue-42"),
            CancellationToken.None).GetAwaiter().GetResult();

        Assert.True(result.Success);
        Assert.Null(gitRepo.CreatedBranch);
        Assert.Contains("agent/issue-42", gitRepo.CheckedOutBranches);
    }

    [Fact]
    public void BranchPreparer_PullFailure_StillSucceeds()
    {
        var gitRepo = new RecordingGitRepository(currentBranch: "main", hasChanges: false, pullThrows: true);
        var preparer = new MissionBranchPreparer(gitRepo);

        var result = preparer.PrepareAsync(
            new BranchPreparationRequest("/workspace/project", "main", "agent/issue-42"),
            CancellationToken.None).GetAwaiter().GetResult();

        Assert.True(result.Success);
        Assert.Equal("agent/issue-42", gitRepo.CreatedBranch);
    }

    // --- CLI integration tests ---

    [Fact]
    public void Execute_CallsBranchPrepWithCorrectParameters()
    {
        using var workspace = TemporaryDirectory.Create();
        var branchPreparer = new FakeMissionBranchPreparer(succeeds: true);
        var services = ExecuteTestServices.Valid(workspace.Path, branchPreparer: branchPreparer);
        SeedPlan(workspace.Path, "42");

        services.Execute(new[] { "execute", "42" });

        Assert.NotNull(branchPreparer.LastRequest);
        Assert.Equal(workspace.Path, branchPreparer.LastRequest!.RepoPath);
        Assert.Equal("main", branchPreparer.LastRequest.DefaultBranch);
        Assert.Equal("agent/issue-42", branchPreparer.LastRequest.MissionBranch);
    }

    [Fact]
    public void Execute_BranchPrepFails_ReturnsBranchPrepError()
    {
        using var workspace = TemporaryDirectory.Create();
        var branchPreparer = new FakeMissionBranchPreparer(succeeds: false, errorMessage: "Cannot switch branches: uncommitted changes");
        var services = ExecuteTestServices.Valid(workspace.Path, branchPreparer: branchPreparer);
        SeedPlan(workspace.Path, "42");

        var result = services.Execute(new[] { "execute", "42" });

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("Branch preparation failed", result.Error);
        Assert.False(services.CodingAgent.Invoked);
    }

    [Fact]
    public void Submit_WhenNotOnExpectedBranch_ReturnsError()
    {
        var gitRepo = new RecordingGitRepository(currentBranch: "main");
        var services = SubmitTestServices.Valid(gitRepo: gitRepo);

        var result = services.Execute(new[] { "submit", "42" });

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("agent/issue-42", result.Error);
        Assert.Null(gitRepo.CreatedBranch);
    }

    [Fact]
    public void RunOnce_BranchPrepFails_MarksMissionAsFailed()
    {
        var branchPreparer = new FakeMissionBranchPreparer(succeeds: false, errorMessage: "uncommitted changes");
        var services = RunOnceTestServices.Valid(branchPreparer: branchPreparer);

        var result = services.Execute(new[] { "run-once" });

        Assert.NotEqual(0, result.ExitCode);
        Assert.Equal(MissionStatus.Failed, services.Missions.Saved.Last().Status);
        Assert.Contains("uncommitted changes", services.Missions.Saved.Last().LastError);
    }

    private static void SeedPlan(string repoPath, string externalId)
    {
        var planDir = Path.Combine(repoPath, ".agent", "runs", $"issue-{externalId}");
        Directory.CreateDirectory(planDir);
        File.WriteAllText(Path.Combine(planDir, "plan.md"), "# Saved plan");
    }
}

internal sealed class FakeMissionBranchPreparer : IMissionBranchPreparer
{
    private readonly bool _succeeds;
    private readonly string? _errorMessage;

    public FakeMissionBranchPreparer(bool succeeds = true, string? errorMessage = null)
    {
        _succeeds = succeeds;
        _errorMessage = errorMessage;
    }

    public BranchPreparationRequest? LastRequest { get; private set; }

    public Task<BranchPreparationResult> PrepareAsync(BranchPreparationRequest request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        return Task.FromResult(_succeeds
            ? BranchPreparationResult.Ok()
            : BranchPreparationResult.Fail(_errorMessage ?? "Branch prep failed."));
    }
}
