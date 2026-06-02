using AgentForeman.Infrastructure.Configuration;

namespace AgentForeman.Tests;

public sealed class ConfigLoaderTests
{
    [Fact]
    public void ValidConfigLoadsSuccessfully()
    {
        using var tempFile = TempConfigFile.Create(ValidConfigYaml);
        var loader = new YamlAgentForemanConfigLoader();

        var result = loader.Load(tempFile.Path);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        Assert.NotNull(result.Config);
        Assert.Equal("elevator-ads-mvp", result.Config.Project.Name);
        Assert.Equal("/home/agent/workspace/elevator-ads-mvp", result.Config.Project.RepoPath);
        Assert.Equal("main", result.Config.Project.DefaultBranch);
        Assert.Equal("github", result.Config.WorkItems.Provider);
        Assert.Equal("claude-cli", result.Config.Planner.Provider);
        Assert.Equal("claude", result.Config.Planner.Command);
        Assert.Equal("codex-cli", result.Config.Executor.Provider);
        Assert.Equal("codex", result.Config.Executor.Command);
        Assert.Equal("agent-blocked", result.Config.WorkItems.BlockedLabel);
        Assert.Equal("agent-failed", result.Config.WorkItems.FailedLabel);
        Assert.Equal(new[] { "dotnet test", "npm --prefix frontend run build" }, result.Config.Tests.Commands);
        Assert.Contains(".env.production", result.Config.Safety.ForbiddenPaths);
        Assert.Contains("too many requests", result.Config.Quota.QuotaPatterns);
        Assert.Equal(30, result.Config.Daemon.PollIntervalSeconds);
    }

    [Fact]
    public void MissingRequiredFieldsProduceValidationErrors()
    {
        using var tempFile = TempConfigFile.Create(
            """
            project:
              repoPath: /home/agent/workspace/elevator-ads-mvp
            workItems:
              repo: caio/elevator-ads-mvp
            planner:
              provider: claude-cli
            executor:
              provider: codex-cli

            """);
        var loader = new YamlAgentForemanConfigLoader();

        var result = loader.Load(tempFile.Path);

        Assert.False(result.IsValid);
        Assert.Null(result.Config);
        Assert.Contains("project.name is required.", result.Errors);
        Assert.Contains("project.defaultBranch is required.", result.Errors);
        Assert.Contains("workItems.provider is required.", result.Errors);
        Assert.Contains("planner.command is required.", result.Errors);
        Assert.Contains("executor.command is required.", result.Errors);
    }

    [Fact]
    public void DaemonPollIntervalSupportsLowercaseKey()
    {
        using var tempFile = TempConfigFile.Create(ValidConfigYaml.Replace(
            "pollIntervalSeconds: 30",
            "pollintervalseconds: 45"));
        var loader = new YamlAgentForemanConfigLoader();

        var result = loader.Load(tempFile.Path);

        Assert.True(result.IsValid);
        Assert.NotNull(result.Config);
        Assert.Equal(45, result.Config.Daemon.PollIntervalSeconds);
    }

    internal const string ValidConfigYaml = """
        project:
          name: elevator-ads-mvp
          repoPath: /home/agent/workspace/elevator-ads-mvp
          defaultBranch: main

        workItems:
          provider: github
          repo: caio/elevator-ads-mvp
          readyLabel: agent-ready
          workingLabel: agent-working
          reviewLabel: agent-review
          pausedLabel: agent-paused
          blockedLabel: agent-blocked
          failedLabel: agent-failed

        planner:
          provider: claude-cli
          command: claude

        executor:
          provider: codex-cli
          command: codex
          sandbox: workspace-write
          approval: never

        tests:
          commands:
            - dotnet test
            - npm --prefix frontend run build

        safety:
          maxFilesChanged: 25
          forbiddenPaths:
            - .env
            - .env.local
            - .env.production
            - appsettings.Production.json
            - secrets/

        quota:
          retryAfterHours: 5
          quotaPatterns:
            - usage limit
            - rate limit
            - quota exceeded
            - try again later
            - limit reached
            - too many requests

        database:
          provider: postgresql
          connectionString: Host=localhost;Port=5432;Database=agent_foreman;Username=agent_foreman;Password=agent_foreman

        daemon:
          pollIntervalSeconds: 30

        """;
}

internal sealed class TempConfigFile : IDisposable
{
    private TempConfigFile(string path)
    {
        Path = path;
    }

    public string Path { get; }

    public static TempConfigFile Create(string content, string fileName = "agent-foreman.yaml")
    {
        var directory = Directory.CreateTempSubdirectory("agent-foreman-tests-");
        var path = System.IO.Path.Combine(directory.FullName, fileName);
        File.WriteAllText(path, content);
        return new TempConfigFile(path);
    }

    public void Dispose()
    {
        var directory = Directory.GetParent(Path);
        directory?.Delete(recursive: true);
    }
}
