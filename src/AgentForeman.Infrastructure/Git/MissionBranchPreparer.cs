using AgentForeman.Core.Git;

namespace AgentForeman.Infrastructure.Git;

public sealed class MissionBranchPreparer : IMissionBranchPreparer
{
    private readonly IGitRepository _gitRepository;

    public MissionBranchPreparer(IGitRepository gitRepository) => _gitRepository = gitRepository;

    public async Task<BranchPreparationResult> PrepareAsync(BranchPreparationRequest request, CancellationToken cancellationToken)
    {
        var currentBranch = await _gitRepository.GetCurrentBranchAsync(request.RepoPath, cancellationToken);
        if (currentBranch == request.MissionBranch)
            return BranchPreparationResult.Ok();

        var status = await _gitRepository.GetStatusAsync(request.RepoPath, cancellationToken);
        if (status.ChangedFiles.Count > 0)
            return BranchPreparationResult.Fail(
                $"Cannot switch to {request.MissionBranch}: {status.ChangedFiles.Count} uncommitted change(s). Stash or commit them first.");

        await _gitRepository.CheckoutAsync(request.RepoPath, request.DefaultBranch, cancellationToken);
        try { await _gitRepository.PullAsync(request.RepoPath, "origin", request.DefaultBranch, cancellationToken); } catch { }

        var branchExists = await _gitRepository.BranchExistsAsync(request.RepoPath, request.MissionBranch, cancellationToken);
        if (branchExists)
            await _gitRepository.CheckoutAsync(request.RepoPath, request.MissionBranch, cancellationToken);
        else
            await _gitRepository.CreateBranchAsync(request.RepoPath, request.MissionBranch, cancellationToken);

        return BranchPreparationResult.Ok();
    }
}
