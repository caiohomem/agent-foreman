namespace AgentForeman.Core.Git;

public interface IMissionBranchPreparer
{
    Task<BranchPreparationResult> PrepareAsync(BranchPreparationRequest request, CancellationToken cancellationToken);
}
