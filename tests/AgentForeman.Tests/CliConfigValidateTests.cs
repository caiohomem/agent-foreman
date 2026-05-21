using AgentForeman.Cli;
using AgentForeman.Infrastructure.Configuration;

namespace AgentForeman.Tests;

public sealed class CliConfigValidateTests
{
    [Fact]
    public void ConfigValidateReturnsZeroForValidConfig()
    {
        using var tempFile = TempConfigFile.Create(ConfigLoaderTests.ValidConfigYaml);

        var result = CliApplication.Execute(
            new[] { "config", "validate", "--config", tempFile.Path },
            new YamlAgentForemanConfigLoader());

        Assert.Equal(0, result.ExitCode);
        Assert.Equal($"Configuration is valid.{Environment.NewLine}", result.Output);
        Assert.Equal(string.Empty, result.Error);
    }

    [Fact]
    public void ConfigValidateReturnsNonZeroForInvalidConfig()
    {
        using var tempFile = TempConfigFile.Create(
            """
            project:
              name: elevator-ads-mvp

            """);

        var result = CliApplication.Execute(
            new[] { "config", "validate", "--config", tempFile.Path },
            new YamlAgentForemanConfigLoader());

        Assert.NotEqual(0, result.ExitCode);
        Assert.Equal(string.Empty, result.Output);
        Assert.Contains("project.repoPath is required.", result.Error);
        Assert.Contains("executor.command is required.", result.Error);
    }

    [Fact]
    public void ConfigValidateUsesDefaultConfigPathWhenConfigOptionIsNotProvided()
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        using var tempDirectory = TempWorkingDirectory.Create();
        File.WriteAllText("agent-foreman.yaml", ConfigLoaderTests.ValidConfigYaml);

        try
        {
            var result = CliApplication.Execute(new[] { "config", "validate" }, new YamlAgentForemanConfigLoader());

            Assert.Equal(0, result.ExitCode);
            Assert.Equal($"Configuration is valid.{Environment.NewLine}", result.Output);
        }
        finally
        {
            Directory.SetCurrentDirectory(currentDirectory);
        }
    }
}

internal sealed class TempWorkingDirectory : IDisposable
{
    private readonly string _previousDirectory;

    private TempWorkingDirectory(string path, string previousDirectory)
    {
        Path = path;
        _previousDirectory = previousDirectory;
    }

    public string Path { get; }

    public static TempWorkingDirectory Create()
    {
        var previousDirectory = Directory.GetCurrentDirectory();
        var directory = Directory.CreateTempSubdirectory("agent-foreman-cwd-");
        Directory.SetCurrentDirectory(directory.FullName);
        return new TempWorkingDirectory(directory.FullName, previousDirectory);
    }

    public void Dispose()
    {
        Directory.SetCurrentDirectory(_previousDirectory);
        Directory.Delete(Path, recursive: true);
    }
}
