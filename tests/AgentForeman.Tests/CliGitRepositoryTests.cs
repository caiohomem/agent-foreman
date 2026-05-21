using AgentForeman.Core.Commands;
using AgentForeman.Infrastructure.Commands;
using AgentForeman.Infrastructure.Git;

namespace AgentForeman.Tests;

public sealed class CliGitRepositoryTests
{
    [Fact]
    public async Task IsRepositoryAsyncReturnsTrueForGitRepo()
    {
        using var repo = await TemporaryGitRepository.CreateAsync();
        if (repo is null)
        {
            return;
        }

        var git = new CliGitRepository(new ProcessCommandRunner());

        Assert.True(await git.IsRepositoryAsync(repo.Path));
    }

    [Fact]
    public async Task IsRepositoryAsyncReturnsFalseForNonGitDirectory()
    {
        if (!GitTestEnvironment.IsGitAvailable())
        {
            return;
        }

        using var directory = TempWorkingDirectory.Create();
        var git = new CliGitRepository(new ProcessCommandRunner());

        Assert.False(await git.IsRepositoryAsync(directory.Path));
    }

    [Fact]
    public async Task GetCurrentBranchAsyncParsesCurrentBranch()
    {
        using var repo = await TemporaryGitRepository.CreateAsync();
        if (repo is null)
        {
            return;
        }

        var git = new CliGitRepository(new ProcessCommandRunner());

        Assert.Equal("main", await git.GetCurrentBranchAsync(repo.Path));
    }

    [Fact]
    public async Task GetStatusAsyncParsesModifiedAndUntrackedFiles()
    {
        using var repo = await TemporaryGitRepository.CreateAsync();
        if (repo is null)
        {
            return;
        }

        File.AppendAllText(System.IO.Path.Combine(repo.Path, "tracked.txt"), "changed");
        await File.WriteAllTextAsync(System.IO.Path.Combine(repo.Path, "new.txt"), "new");
        var git = new CliGitRepository(new ProcessCommandRunner());

        var status = await git.GetStatusAsync(repo.Path);

        Assert.Contains(status.ChangedFiles, file => file.Path == "tracked.txt" && file.IsModified);
        Assert.Contains(status.ChangedFiles, file => file.Path == "new.txt" && file.IsUntracked);
    }

    [Fact]
    public async Task BranchExistsAsyncDetectsExistingAndMissingBranches()
    {
        using var repo = await TemporaryGitRepository.CreateAsync();
        if (repo is null)
        {
            return;
        }

        var git = new CliGitRepository(new ProcessCommandRunner());

        Assert.True(await git.BranchExistsAsync(repo.Path, "main"));
        Assert.False(await git.BranchExistsAsync(repo.Path, "missing"));
    }

    [Fact]
    public async Task GetDiffAsyncReturnsDiffText()
    {
        using var repo = await TemporaryGitRepository.CreateAsync();
        if (repo is null)
        {
            return;
        }

        File.AppendAllText(System.IO.Path.Combine(repo.Path, "tracked.txt"), "changed");
        var git = new CliGitRepository(new ProcessCommandRunner());

        var diff = await git.GetDiffAsync(repo.Path);

        Assert.Contains("tracked.txt", diff);
    }

    [Fact]
    public async Task CommitAsyncHandlesNoChangesGracefully()
    {
        using var repo = await TemporaryGitRepository.CreateAsync();
        if (repo is null)
        {
            return;
        }

        var git = new CliGitRepository(new ProcessCommandRunner());

        var result = await git.CommitAsync(repo.Path, "nothing");

        Assert.False(result.Created);
        Assert.Contains("nothing to commit", result.Output, StringComparison.OrdinalIgnoreCase);
    }
}

internal static class GitTestEnvironment
{
    public static bool IsGitAvailable()
    {
        try
        {
            using var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "git",
                ArgumentList = { "--version" },
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            });
            process?.WaitForExit();
            return process?.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}

internal sealed class TemporaryGitRepository : IDisposable
{
    private TemporaryGitRepository(string path)
    {
        Path = path;
    }

    public string Path { get; }

    public static async Task<TemporaryGitRepository?> CreateAsync()
    {
        if (!GitTestEnvironment.IsGitAvailable())
        {
            return null;
        }

        var directory = Directory.CreateTempSubdirectory("agent-foreman-git-");
        var runner = new ProcessCommandRunner();

        await runner.RunAsync(new CommandRequest("git", new[] { "init", "-b", "main" }, WorkingDirectory: directory.FullName));
        await File.WriteAllTextAsync(System.IO.Path.Combine(directory.FullName, "tracked.txt"), "initial");
        await runner.RunAsync(new CommandRequest("git", new[] { "config", "user.email", "agent@example.invalid" }, WorkingDirectory: directory.FullName));
        await runner.RunAsync(new CommandRequest("git", new[] { "config", "user.name", "Agent Foreman Tests" }, WorkingDirectory: directory.FullName));
        await runner.RunAsync(new CommandRequest("git", new[] { "add", "." }, WorkingDirectory: directory.FullName));
        await runner.RunAsync(new CommandRequest("git", new[] { "commit", "-m", "initial" }, WorkingDirectory: directory.FullName));

        return new TemporaryGitRepository(directory.FullName);
    }

    public void Dispose()
    {
        Directory.Delete(Path, recursive: true);
    }
}
