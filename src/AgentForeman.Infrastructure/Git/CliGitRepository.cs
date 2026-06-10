using AgentForeman.Core.Commands;
using AgentForeman.Core.Git;

namespace AgentForeman.Infrastructure.Git;

public sealed class CliGitRepository : IGitRepository
{
    private readonly ICommandRunner _commandRunner;

    public CliGitRepository(ICommandRunner commandRunner)
    {
        _commandRunner = commandRunner;
    }

    public async Task<bool> IsRepositoryAsync(string repoPath, CancellationToken cancellationToken = default)
    {
        var result = await RunGitAsync(repoPath, new[] { "rev-parse", "--is-inside-work-tree" }, cancellationToken);
        return result.Success && result.StdoutText.Trim().Equals("true", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<string> GetCurrentBranchAsync(string repoPath, CancellationToken cancellationToken = default)
    {
        var result = await RunGitAsync(repoPath, new[] { "branch", "--show-current" }, cancellationToken);
        return result.StdoutText.Trim();
    }

    public async Task CheckoutAsync(string repoPath, string branch, CancellationToken cancellationToken = default)
    {
        await RunGitAsync(repoPath, new[] { "checkout", branch }, cancellationToken);
    }

    public async Task PullAsync(string repoPath, string remote, string branch, CancellationToken cancellationToken = default)
    {
        await RunGitAsync(repoPath, new[] { "pull", remote, branch }, cancellationToken);
    }

    public async Task CreateBranchAsync(string repoPath, string branch, CancellationToken cancellationToken = default)
    {
        await RunGitAsync(repoPath, new[] { "checkout", "-b", branch }, cancellationToken);
    }

    public async Task<bool> BranchExistsAsync(string repoPath, string branch, CancellationToken cancellationToken = default)
    {
        var result = await RunGitAsync(repoPath, new[] { "rev-parse", "--verify", branch }, cancellationToken);
        return result.Success;
    }

    public async Task<GitStatusResult> GetStatusAsync(string repoPath, CancellationToken cancellationToken = default)
    {
        var result = await RunGitAsync(repoPath, new[] { "status", "--porcelain" }, cancellationToken);
        return new GitStatusResult(ParseStatus(result.StdoutText));
    }

    public async Task<string> GetDiffAsync(string repoPath, CancellationToken cancellationToken = default)
    {
        var result = await RunGitAsync(repoPath, new[] { "diff" }, cancellationToken);
        return result.StdoutText;
    }

    public async Task<bool> HasOnlyLineEndingChangesAsync(string repoPath, CancellationToken cancellationToken = default)
    {
        var result = await RunGitAsync(repoPath, new[] { "diff", "--quiet", "--ignore-cr-at-eol", "--exit-code" }, cancellationToken);
        return result.ExitCode == 0;
    }

    public async Task AddAllAsync(string repoPath, CancellationToken cancellationToken = default)
    {
        await RunGitAsync(repoPath, new[] { "add", "--all" }, cancellationToken);
    }

    public async Task<GitCommitResult> CommitAsync(string repoPath, string message, CancellationToken cancellationToken = default)
    {
        var result = await RunGitAsync(repoPath, new[] { "commit", "-m", message }, cancellationToken);
        var output = result.StdoutText + result.StderrText;
        if (!result.Success && output.Contains("nothing to commit", StringComparison.OrdinalIgnoreCase))
        {
            return new GitCommitResult(Created: false, output);
        }

        return new GitCommitResult(result.Success, output);
    }

    public async Task PushAsync(string repoPath, string remote, string branch, CancellationToken cancellationToken = default)
    {
        var result = await RunGitAsync(repoPath, new[] { "push", remote, branch }, cancellationToken);
        if (!result.Success)
        {
            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(result.StderrText) ? "git push failed." : result.StderrText.Trim());
        }
    }

    public async Task<bool> StashAsync(string repoPath, string message, CancellationToken cancellationToken = default)
    {
        var result = await RunGitAsync(repoPath, new[] { "stash", "push", "-u", "-m", message }, cancellationToken);
        return result.Success;
    }

    private Task<CommandResult> RunGitAsync(string repoPath, IReadOnlyList<string> arguments, CancellationToken cancellationToken)
    {
        return _commandRunner.RunAsync(
            new CommandRequest("git", arguments, WorkingDirectory: repoPath),
            cancellationToken: cancellationToken);
    }

    private static IReadOnlyList<GitChangedFile> ParseStatus(string statusText)
    {
        var files = new List<GitChangedFile>();
        foreach (var rawLine in statusText.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries))
        {
            if (rawLine.Length < 4)
            {
                continue;
            }

            var statusCode = rawLine[..2];
            var path = rawLine[3..];
            var renameSeparator = path.IndexOf(" -> ", StringComparison.Ordinal);
            if (renameSeparator >= 0)
            {
                path = path[(renameSeparator + 4)..];
            }

            files.Add(new GitChangedFile(statusCode, path));
        }

        return files;
    }
}
