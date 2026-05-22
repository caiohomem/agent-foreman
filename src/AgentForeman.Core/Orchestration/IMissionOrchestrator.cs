namespace AgentForeman.Core.Orchestration;

public interface IMissionOrchestrator
{
    Task<RunOnceResult> RunOnceAsync(RunOnceRequest request, Action<string>? onProgress, CancellationToken cancellationToken);
    Task<ResumeResult> ResumeAsync(ResumeRequest request, Action<string>? onProgress, CancellationToken cancellationToken);
    Task<DaemonTickResult> DaemonTickAsync(DaemonTickRequest request, Action<string>? onProgress, CancellationToken cancellationToken);
}
