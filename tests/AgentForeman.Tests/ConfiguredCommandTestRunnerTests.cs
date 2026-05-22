using AgentForeman.Core.Commands;
using AgentForeman.Core.Testing;
using AgentForeman.Infrastructure.Testing;

namespace AgentForeman.Tests;

public sealed class ConfiguredCommandTestRunnerTests
{
    [Fact]
    public async Task RunsAllCommandsWhenTheyAllPass()
    {
        using var workspace = TemporaryDirectory.Create();
        var runner = new RecordingCommandRunner(string.Empty, string.Empty);
        var testRunner = new ConfiguredCommandTestRunner(runner);
        var request = new TestRunRequest("42", workspace.Path, new[] { "dotnet test", "npm run build" }, workspace.Path);

        var result = await testRunner.RunAsync(request, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(2, result.CommandResults.Count);
        Assert.Equal(2, runner.Requests.Count);
        Assert.Equal("dotnet", runner.Requests[0].Executable);
        Assert.Equal(new[] { "test" }, runner.Requests[0].Arguments);
        Assert.Equal("npm", runner.Requests[1].Executable);
        Assert.Equal(new[] { "run", "build" }, runner.Requests[1].Arguments);
    }

    [Fact]
    public async Task StopsOnFirstFailure()
    {
        using var workspace = TemporaryDirectory.Create();
        var runner = new FailingOnNthCommandRunner(failOnIndex: 0);
        var testRunner = new ConfiguredCommandTestRunner(runner);
        var request = new TestRunRequest("42", workspace.Path, new[] { "dotnet test", "npm run build" }, workspace.Path);

        var result = await testRunner.RunAsync(request, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Single(result.CommandResults);
        Assert.False(result.CommandResults[0].Success);
        Assert.Equal(1, runner.InvocationCount);
        Assert.Contains("dotnet test", result.ErrorMessage);
    }

    [Fact]
    public async Task TestsLogPathIsGeneratedCorrectly()
    {
        using var workspace = TemporaryDirectory.Create();
        var testRunner = new ConfiguredCommandTestRunner(new RecordingCommandRunner(string.Empty));
        var request = new TestRunRequest("42", workspace.Path, new[] { "dotnet test" }, workspace.Path);

        var result = await testRunner.RunAsync(request, CancellationToken.None);

        Assert.Equal(Path.Combine(workspace.Path, "tests.log"), result.LogPath);
        Assert.True(File.Exists(result.LogPath));
    }

    [Theory]
    [InlineData("dotnet test", "dotnet", new[] { "test" })]
    [InlineData("npm --prefix frontend run build", "npm", new[] { "--prefix", "frontend", "run", "build" })]
    public void ParseCommandHandlesCommonFormats(string commandLine, string expectedExecutable, string[] expectedArgs)
    {
        var (executable, arguments) = ConfiguredCommandTestRunner.ParseCommand(commandLine);

        Assert.Equal(expectedExecutable, executable);
        Assert.Equal(expectedArgs, arguments);
    }
}

internal sealed class FailingOnNthCommandRunner : ICommandRunner
{
    private readonly int _failOnIndex;
    public int InvocationCount { get; private set; }

    public FailingOnNthCommandRunner(int failOnIndex)
    {
        _failOnIndex = failOnIndex;
    }

    public Task<AgentForeman.Core.Commands.CommandResult> RunAsync(
        CommandRequest request,
        Action<CommandOutputLine>? onOutputLine = null,
        CancellationToken cancellationToken = default)
    {
        var index = InvocationCount++;
        var exitCode = index == _failOnIndex ? 1 : 0;
        return Task.FromResult(new AgentForeman.Core.Commands.CommandResult(
            exitCode,
            string.Empty,
            exitCode != 0 ? "command failed" : string.Empty,
            string.Empty,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow));
    }
}
