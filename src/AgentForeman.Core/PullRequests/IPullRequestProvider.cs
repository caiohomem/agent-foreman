namespace AgentForeman.Core.PullRequests;

public interface IPullRequestProvider
{
    Task<PullRequestResult> CreateAsync(PullRequestRequest request, CancellationToken cancellationToken);
    Task<PullRequestResult> EnableAutoMergeAsync(PullRequestAutoMergeRequest request, CancellationToken cancellationToken);
}

public sealed record PullRequestAutoMergeRequest(
    string Repository,
    string PullRequestUrl,
    string Method);
