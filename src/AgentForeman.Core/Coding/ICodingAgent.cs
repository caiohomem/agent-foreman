namespace AgentForeman.Core.Coding;

public interface ICodingAgent
{
    Task<CodingResult> ExecuteAsync(CodingRequest request, CancellationToken cancellationToken);
}
