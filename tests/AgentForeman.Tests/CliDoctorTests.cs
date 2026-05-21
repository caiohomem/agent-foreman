using AgentForeman.Cli;
using AgentForeman.Core.Configuration;
using AgentForeman.Core.Prerequisites;

namespace AgentForeman.Tests;

public sealed class CliDoctorTests
{
    [Fact]
    public void DoctorReturnsZeroWhenAllChecksPass()
    {
        var services = DoctorTestServices.Valid();

        var result = CliApplication.Execute(new[] { "doctor" }, services.ConfigLoader, services.RepositoryChecker, services.CommandChecker);

        Assert.Equal(0, result.ExitCode);
        Assert.Equal(string.Empty, result.Error);
        Assert.Contains("Agent Foreman Doctor", result.Output);
        Assert.Contains("[OK] Config file found: agent-foreman.yaml", result.Output);
        Assert.Contains("[OK] Configuration is valid.", result.Output);
        Assert.Contains("[OK] Repo path exists: /workspace/project", result.Output);
        Assert.Contains("[OK] Repo path is a git repository.", result.Output);
        Assert.Contains("[OK] git found.", result.Output);
        Assert.Contains("[OK] gh found.", result.Output);
        Assert.Contains("[OK] claude found.", result.Output);
        Assert.Contains("[OK] codex found.", result.Output);
        Assert.Contains("All checks passed.", result.Output);
    }

    [Fact]
    public void DoctorReturnsNonZeroWhenConfigIsMissingOrInvalid()
    {
        var services = DoctorTestServices.InvalidConfig("Config file not found: custom.yaml");

        var result = CliApplication.Execute(new[] { "doctor", "--config", "custom.yaml" }, services.ConfigLoader, services.RepositoryChecker, services.CommandChecker);

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("[FAIL] Config file found: custom.yaml", result.Output);
        Assert.Contains("[FAIL] Config file not found: custom.yaml", result.Output);
        Assert.Contains("One or more checks failed.", result.Output);
    }

    [Fact]
    public void DoctorReturnsNonZeroWhenRepoPathDoesNotExist()
    {
        var services = DoctorTestServices.Valid(repoPathExists: false);

        var result = CliApplication.Execute(new[] { "doctor" }, services.ConfigLoader, services.RepositoryChecker, services.CommandChecker);

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("[FAIL] Repo path exists: /workspace/project", result.Output);
        Assert.Contains("One or more checks failed.", result.Output);
    }

    [Fact]
    public void DoctorReturnsNonZeroWhenRepoPathIsNotGitRepository()
    {
        var services = DoctorTestServices.Valid(isGitRepository: false);

        var result = CliApplication.Execute(new[] { "doctor" }, services.ConfigLoader, services.RepositoryChecker, services.CommandChecker);

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("[FAIL] Repo path is a git repository.", result.Output);
        Assert.Contains("One or more checks failed.", result.Output);
    }

    [Fact]
    public void DoctorReturnsNonZeroWhenRequiredCommandIsMissing()
    {
        var services = DoctorTestServices.Valid(missingCommand: "codex");

        var result = CliApplication.Execute(new[] { "doctor" }, services.ConfigLoader, services.RepositoryChecker, services.CommandChecker);

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("[FAIL] codex not found in PATH.", result.Output);
        Assert.Contains("One or more checks failed.", result.Output);
    }
}

internal sealed record DoctorTestServices(
    IAgentForemanConfigLoader ConfigLoader,
    IRepositoryChecker RepositoryChecker,
    ICommandAvailabilityChecker CommandChecker)
{
    public static DoctorTestServices Valid(
        bool repoPathExists = true,
        bool isGitRepository = true,
        string? missingCommand = null)
    {
        var config = new AgentForemanConfig
        {
            Project = new ProjectConfig
            {
                Name = "project",
                RepoPath = "/workspace/project",
                DefaultBranch = "main",
            },
            WorkItems = new WorkItemsConfig
            {
                Provider = "github",
            },
            Planner = new PlannerConfig
            {
                Provider = "claude-cli",
                Command = "claude",
            },
            Executor = new ExecutorConfig
            {
                Provider = "codex-cli",
                Command = "codex",
            },
            Database = new DatabaseConfig
            {
                Provider = "postgresql",
                ConnectionString = "Host=localhost;Port=5432;Database=agent_foreman;Username=agent_foreman;Password=agent_foreman",
            },
        };

        return new DoctorTestServices(
            new FakeConfigLoader(AgentForemanConfigLoadResult.Success(config)),
            new FakeRepositoryChecker(repoPathExists, isGitRepository),
            new FakeCommandAvailabilityChecker(missingCommand));
    }

    public static DoctorTestServices InvalidConfig(string error)
    {
        return new DoctorTestServices(
            new FakeConfigLoader(AgentForemanConfigLoadResult.Failure(new[] { error })),
            new FakeRepositoryChecker(repoPathExists: true, isGitRepository: true),
            new FakeCommandAvailabilityChecker(missingCommand: null));
    }
}

internal sealed class FakeConfigLoader : IAgentForemanConfigLoader
{
    private readonly AgentForemanConfigLoadResult _result;

    public FakeConfigLoader(AgentForemanConfigLoadResult result)
    {
        _result = result;
    }

    public AgentForemanConfigLoadResult Load(string path)
    {
        return _result;
    }
}

internal sealed class FakeRepositoryChecker : IRepositoryChecker
{
    private readonly bool _repoPathExists;
    private readonly bool _isGitRepository;

    public FakeRepositoryChecker(bool repoPathExists, bool isGitRepository)
    {
        _repoPathExists = repoPathExists;
        _isGitRepository = isGitRepository;
    }

    public bool DirectoryExists(string path)
    {
        return _repoPathExists;
    }

    public bool IsGitRepository(string path)
    {
        return _isGitRepository;
    }
}

internal sealed class FakeCommandAvailabilityChecker : ICommandAvailabilityChecker
{
    private readonly string? _missingCommand;

    public FakeCommandAvailabilityChecker(string? missingCommand)
    {
        _missingCommand = missingCommand;
    }

    public bool IsAvailable(string command)
    {
        return command != _missingCommand;
    }
}
