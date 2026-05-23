using AgentForeman.Cli;
using AgentForeman.Core.Commands;
using AgentForeman.Core.Configuration;
using AgentForeman.Core.Git;
using AgentForeman.Core.Prerequisites;
using AgentForeman.Core.State;

namespace AgentForeman.Tests;

public sealed class CliGitTests
{
    [Fact]
    public void GitStatusCommandUsesConfiguredRepoPath()
    {
        var git = new FakeGitRepository
        {
            IsRepositoryResult = true,
            CurrentBranch = "main",
            Status = new GitStatusResult(new[] { new GitChangedFile(" M", "src/app.cs") }),
        };
        var services = DoctorTestServices.Valid();

        var result = CliApplication.Execute(
            new[] { "git", "status" },
            services.ConfigLoader,
            services.RepositoryChecker,
            services.CommandChecker,
            new FakeStateStore(),
            new FakeCommandRunner(),
            gitRepository: git);

        Assert.Equal(0, result.ExitCode);
        Assert.Equal("/workspace/project", git.LastStatusRepoPath);
        Assert.Contains("Branch: main", result.Output);
        Assert.Contains(" M src/app.cs", result.Output);
    }

    [Fact]
    public void GitStatusCommandReturnsNonZeroWhenConfiguredPathIsNotGitRepository()
    {
        var git = new FakeGitRepository { IsRepositoryResult = false };
        var services = DoctorTestServices.Valid();

        var result = CliApplication.Execute(
            new[] { "git", "status" },
            services.ConfigLoader,
            services.RepositoryChecker,
            services.CommandChecker,
            new FakeStateStore(),
            new FakeCommandRunner(),
            gitRepository: git);

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("Configured repoPath is not a git repository: /workspace/project", result.Error);
    }
}

internal sealed class FakeGitRepository : IGitRepository
{
    public bool IsRepositoryResult { get; init; }
    public string CurrentBranch { get; init; } = string.Empty;
    public GitStatusResult Status { get; init; } = new(Array.Empty<GitChangedFile>());
    public string Diff { get; init; } = string.Empty;
    public string? LastStatusRepoPath { get; private set; }

    public Task<bool> IsRepositoryAsync(string repoPath, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(IsRepositoryResult);
    }

    public Task<string> GetCurrentBranchAsync(string repoPath, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(CurrentBranch);
    }

    public Task CheckoutAsync(string repoPath, string branch, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task PullAsync(string repoPath, string remote, string branch, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task CreateBranchAsync(string repoPath, string branch, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task<bool> BranchExistsAsync(string repoPath, string branch, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(false);
    }

    public Task<GitStatusResult> GetStatusAsync(string repoPath, CancellationToken cancellationToken = default)
    {
        LastStatusRepoPath = repoPath;
        return Task.FromResult(Status);
    }

    public Task<string> GetDiffAsync(string repoPath, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Diff);
    }

    public Task<bool> HasOnlyLineEndingChangesAsync(string repoPath, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(false);
    }

    public Task AddAllAsync(string repoPath, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task<GitCommitResult> CommitAsync(string repoPath, string message, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new GitCommitResult(Created: true, Output: string.Empty));
    }

    public Task PushAsync(string repoPath, string remote, string branch, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}

internal sealed class FakeCommandRunner : ICommandRunner
{
    public Task<AgentForeman.Core.Commands.CommandResult> RunAsync(
        CommandRequest request,
        Action<CommandOutputLine>? onOutputLine = null,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new AgentForeman.Core.Commands.CommandResult(
            0,
            string.Empty,
            string.Empty,
            string.Empty,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow));
    }
}
