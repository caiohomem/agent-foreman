namespace AgentForeman.Core.PullRequests;

public interface IPullRequestProvider
{
    Task<PullRequestResult> CreateAsync(PullRequestRequest request, CancellationToken cancellationToken);
}
