using AgentForeman.Cli;
using AgentForeman.Core.Configuration;
using AgentForeman.Core.Events;
using AgentForeman.Core.Safety;
using AgentForeman.Core.State;
using AgentForeman.Core.Testing;

namespace AgentForeman.Tests;

public sealed class CliVerifyTests
{
    [Fact]
    public void VerifyMarksMissionAsTestsPassedWhenAllChecksPass()
    {
        using var workspace = TemporaryDirectory.Create();
        var services = VerifyTestServices.Valid(workspace.Path);

        var result = services.Execute(new[] { "verify", "42" });

        Assert.Equal(0, result.ExitCode);
        Assert.Equal(MissionStatus.TestsPassed, services.Missions.Saved.Last().Status);
        Assert.Contains("Verification passed.", result.Output);
        Assert.Contains("tests.log", result.Output);
    }

    [Fact]
    public void VerifyMarksMissionAsTestsFailedWhenTestsFail()
    {
        using var workspace = TemporaryDirectory.Create();
        var services = VerifyTestServices.Valid(workspace.Path,
            testResult: new TestRunResult(
                false,
                Path.Combine(workspace.Path, ".agent", "runs", "issue-42", "tests.log"),
                new[] { new TestCommandResult("dotnet test", 1, string.Empty, "test failed", TimeSpan.Zero) },
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow,
                "Command failed (exit 1): dotnet test"));

        var result = services.Execute(new[] { "verify", "42" });

        Assert.NotEqual(0, result.ExitCode);
        Assert.Equal(MissionStatus.TestsFailed, services.Missions.Saved.Last().Status);
        Assert.Contains("dotnet test", services.Missions.Saved.Last().LastError);
    }

    [Fact]
    public void VerifyMarksMissionAsFailedWhenSafetyFails()
    {
        using var workspace = TemporaryDirectory.Create();
        var services = VerifyTestServices.Valid(workspace.Path,
            safetyResult: SafetyCheckResult.Fail(new[]
            {
                new SafetyViolation("Too many changed files: 30 (max 100)."),
            }));

        var result = services.Execute(new[] { "verify", "42" });

        Assert.NotEqual(0, result.ExitCode);
        Assert.Equal(MissionStatus.Failed, services.Missions.Saved.Last().Status);
        Assert.Contains("Too many changed files", result.Error);
        Assert.False(services.TestRunner.Invoked);
    }

    [Fact]
    public void VerifyFetchesWorkItemById()
    {
        using var workspace = TemporaryDirectory.Create();
        var services = VerifyTestServices.Valid(workspace.Path);

        services.Execute(new[] { "verify", "42" });

        Assert.Equal("42", services.WorkItems.RequestedId);
    }

    [Fact]
    public void VerifyReturnsNonZeroWhenWorkItemDoesNotExist()
    {
        using var workspace = TemporaryDirectory.Create();
        var services = VerifyTestServices.Valid(workspace.Path, workItemExists: false);

        var result = services.Execute(new[] { "verify", "404" });

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("Work item not found", result.Error);
    }

    [Fact]
    public void VerifyRecordsVerificationStartedAndCompletedEvents()
    {
        using var workspace = TemporaryDirectory.Create();
        var services = VerifyTestServices.Valid(workspace.Path);

        var result = services.Execute(new[] { "verify", "42" });

        Assert.Equal(0, result.ExitCode);
        Assert.Contains(services.Events.Events, e => e.EventType == MissionEventType.VerificationStarted);
        Assert.Contains(services.Events.Events, e => e.EventType == MissionEventType.VerificationCompleted);
    }
}

internal sealed class VerifyTestServices
{
    private readonly FakeConfigLoader _configLoader;

    private VerifyTestServices(
        FakeConfigLoader configLoader,
        FakeWorkItemProvider workItems,
        FakeMissionRepository missions,
        FakeTestRunner testRunner,
        FakeSafetyChecker safetyChecker,
        FakeMissionEventRecorder events)
    {
        _configLoader = configLoader;
        WorkItems = workItems;
        Missions = missions;
        TestRunner = testRunner;
        SafetyChecker = safetyChecker;
        Events = events;
    }

    public FakeWorkItemProvider WorkItems { get; }
    public FakeMissionRepository Missions { get; }
    public FakeTestRunner TestRunner { get; }
    public FakeSafetyChecker SafetyChecker { get; }
    public FakeMissionEventRecorder Events { get; }

    public static VerifyTestServices Valid(
        string repoPath,
        TestRunResult? testResult = null,
        SafetyCheckResult? safetyResult = null,
        bool workItemExists = true)
    {
        var config = new AgentForemanConfig
        {
            Project = new ProjectConfig
            {
                Name = "project",
                RepoPath = repoPath,
                DefaultBranch = "main",
            },
            WorkItems = new WorkItemsConfig
            {
                Provider = "github",
                Repo = "caio/elevator-ads-mvp",
            },
            Tests = new TestsConfig
            {
                Commands = new[] { "dotnet test" },
            },
            Safety = new SafetyConfig
            {
                MaxFilesChanged = 100,
                ForbiddenPaths = new[] { ".env", "secrets/" },
            },
            Database = new DatabaseConfig
            {
                Provider = "postgresql",
                ConnectionString = "Host=localhost;Database=agent_foreman",
            },
        };

        var defaultTestResult = new TestRunResult(
            true,
            Path.Combine(repoPath, ".agent", "runs", "issue-42", "tests.log"),
            new[] { new TestCommandResult("dotnet test", 0, "All tests passed.", string.Empty, TimeSpan.Zero) },
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            null);

        return new VerifyTestServices(
            new FakeConfigLoader(AgentForemanConfigLoadResult.Success(config)),
            new FakeWorkItemProvider(workItemExists),
            new FakeMissionRepository(),
            new FakeTestRunner(testResult ?? defaultTestResult),
            new FakeSafetyChecker(safetyResult ?? SafetyCheckResult.Ok()),
            new FakeMissionEventRecorder());
    }

    public CommandResult Execute(IReadOnlyList<string> args)
    {
        var doctorServices = DoctorTestServices.Valid();
        return CliApplication.Execute(
            args,
            _configLoader,
            doctorServices.RepositoryChecker,
            doctorServices.CommandChecker,
            new FakeStateStore(),
            new RecordingCommandRunner(),
            workItemProvider: WorkItems,
            missionRepository: Missions,
            testRunner: TestRunner,
            safetyChecker: SafetyChecker,
            missionEventRecorder: Events);
    }
}

internal sealed class FakeTestRunner : ITestRunner
{
    private readonly TestRunResult _result;

    public FakeTestRunner(TestRunResult result)
    {
        _result = result;
    }

    public TestRunRequest? LastRequest { get; private set; }
    public bool Invoked => LastRequest is not null;

    public Task<TestRunResult> RunAsync(TestRunRequest request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        return Task.FromResult(_result);
    }
}

internal sealed class FakeSafetyChecker : ISafetyChecker
{
    private readonly SafetyCheckResult _result;

    public FakeSafetyChecker(SafetyCheckResult result)
    {
        _result = result;
    }

    public Task<SafetyCheckResult> CheckAsync(string repoPath, AgentForeman.Core.Configuration.SafetyConfig safetyConfig, CancellationToken cancellationToken)
    {
        return Task.FromResult(_result);
    }
}
