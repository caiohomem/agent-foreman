namespace AgentForeman.Core.Git;

public interface IGitRepository
{
    Task<bool> IsRepositoryAsync(string repoPath, CancellationToken cancellationToken = default);
    Task<string> GetCurrentBranchAsync(string repoPath, CancellationToken cancellationToken = default);
    Task CheckoutAsync(string repoPath, string branch, CancellationToken cancellationToken = default);
    Task PullAsync(string repoPath, string remote, string branch, CancellationToken cancellationToken = default);
    Task CreateBranchAsync(string repoPath, string branch, CancellationToken cancellationToken = default);
    Task<bool> BranchExistsAsync(string repoPath, string branch, CancellationToken cancellationToken = default);
    Task<GitStatusResult> GetStatusAsync(string repoPath, CancellationToken cancellationToken = default);
    Task<string> GetDiffAsync(string repoPath, CancellationToken cancellationToken = default);
    Task AddAllAsync(string repoPath, CancellationToken cancellationToken = default);
    Task<GitCommitResult> CommitAsync(string repoPath, string message, CancellationToken cancellationToken = default);
    Task PushAsync(string repoPath, string remote, string branch, CancellationToken cancellationToken = default);
}
