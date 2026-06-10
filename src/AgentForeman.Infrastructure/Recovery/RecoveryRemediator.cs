using AgentForeman.Core.Git;
using AgentForeman.Core.Recovery;

namespace AgentForeman.Infrastructure.Recovery;

public sealed class RecoveryRemediator : IRecoveryRemediator
{
    private readonly IGitRepository _gitRepository;
    public RecoveryRemediator(IGitRepository gitRepository) => _gitRepository = gitRepository;

    public async Task<RemediationResult> RemediateAsync(RecoveryDiagnosis diagnosis, RemediationContext context, CancellationToken cancellationToken)
    {
        return diagnosis.Category switch
        {
            RecoveryCategory.DirtyWorktree => await StashAsync(context, cancellationToken),
            RecoveryCategory.Transient => new(true, true, false, false, null),
            RecoveryCategory.Quota => new(true, false, true, false, null),
            RecoveryCategory.CodeError when context.FailedStage == FailedStage.Tests => new(true, false, false, true, null),
            _ => new(false, false, false, false, diagnosis.Diagnosis),
        };
    }

    private async Task<RemediationResult> StashAsync(RemediationContext context, CancellationToken cancellationToken)
    {
        var message = $"agent-foreman/recovery-{context.MissionId}-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}";
        var success = await _gitRepository.StashAsync(context.RepoPath, message, cancellationToken);
        return new(success, success, false, false, success ? null : "git stash failed.");
    }
}
