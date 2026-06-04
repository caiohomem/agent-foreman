using AgentForeman.Core.Configuration;
using AgentForeman.Core.Git;
using AgentForeman.Infrastructure.Safety;

namespace AgentForeman.Tests;

public sealed class GitSafetyCheckerTests
{
    [Fact]
    public async Task DetectsTooManyChangedFiles()
    {
        var git = new FakeGitRepository
        {
            IsRepositoryResult = true,
            Status = new GitStatusResult(new[]
            {
                new GitChangedFile(" M", "a.cs"),
                new GitChangedFile(" M", "b.cs"),
                new GitChangedFile(" M", "c.cs"),
            }),
        };
        var checker = new GitSafetyChecker(git);

        var result = await checker.CheckAsync("/repo", new SafetyConfig { MaxFilesChanged = 2 }, CancellationToken.None);

        Assert.False(result.Passed);
        Assert.Single(result.Violations);
        Assert.Contains("Too many changed files", result.Violations[0].Message);
        Assert.Contains("3", result.Violations[0].Message);
    }

    [Fact]
    public async Task DetectsForbiddenExactFile()
    {
        var git = new FakeGitRepository
        {
            IsRepositoryResult = true,
            Status = new GitStatusResult(new[] { new GitChangedFile(" M", ".env") }),
        };
        var checker = new GitSafetyChecker(git);

        var result = await checker.CheckAsync("/repo", new SafetyConfig { ForbiddenPaths = new[] { ".env" } }, CancellationToken.None);

        Assert.False(result.Passed);
        Assert.Single(result.Violations);
        Assert.Contains(".env", result.Violations[0].Message);
    }

    [Fact]
    public async Task DetectsForbiddenDirectoryPrefix()
    {
        var git = new FakeGitRepository
        {
            IsRepositoryResult = true,
            Status = new GitStatusResult(new[] { new GitChangedFile(" M", "secrets/api-key.txt") }),
        };
        var checker = new GitSafetyChecker(git);

        var result = await checker.CheckAsync("/repo", new SafetyConfig { ForbiddenPaths = new[] { "secrets/" } }, CancellationToken.None);

        Assert.False(result.Passed);
        Assert.Single(result.Violations);
        Assert.Contains("secrets/api-key.txt", result.Violations[0].Message);
    }

    [Fact]
    public async Task AllowsSafeChangedFiles()
    {
        var git = new FakeGitRepository
        {
            IsRepositoryResult = true,
            Status = new GitStatusResult(new[]
            {
                new GitChangedFile(" M", "src/App.cs"),
                new GitChangedFile(" M", "tests/AppTests.cs"),
            }),
        };
        var checker = new GitSafetyChecker(git);

        var result = await checker.CheckAsync("/repo",
            new SafetyConfig
            {
                MaxFilesChanged = 100,
                ForbiddenPaths = new[] { ".env", "secrets/" },
            },
            CancellationToken.None);

        Assert.True(result.Passed);
        Assert.Empty(result.Violations);
    }

    [Fact]
    public async Task DoesNotFlagPartialFileNameMatch()
    {
        var git = new FakeGitRepository
        {
            IsRepositoryResult = true,
            Status = new GitStatusResult(new[] { new GitChangedFile(" M", ".env.example") }),
        };
        var checker = new GitSafetyChecker(git);

        var result = await checker.CheckAsync("/repo", new SafetyConfig { ForbiddenPaths = new[] { ".env" } }, CancellationToken.None);

        Assert.True(result.Passed);
    }
}
