namespace AgentForeman.Core.Recovery;

public interface IRecoveryRemediator
{
    Task<RemediationResult> RemediateAsync(RecoveryDiagnosis diagnosis, RemediationContext context, CancellationToken cancellationToken);
}
