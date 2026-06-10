namespace AgentForeman.Core.Recovery;

public interface IRecoveryAgent
{
    Task<RecoveryDiagnosis> DiagnoseAsync(RecoveryRequest request, CancellationToken cancellationToken);
}
