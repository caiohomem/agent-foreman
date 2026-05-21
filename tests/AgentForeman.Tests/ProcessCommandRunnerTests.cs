using AgentForeman.Core.Commands;
using AgentForeman.Infrastructure.Commands;

namespace AgentForeman.Tests;

public sealed class ProcessCommandRunnerTests
{
    [Fact]
    public async Task SuccessfulCommandReturnsExitCodeZero()
    {
        var result = await RunHelperAsync("stdout", "hello");

        Assert.Equal(0, result.ExitCode);
        Assert.True(result.Success);
    }

    [Fact]
    public async Task FailedCommandReturnsNonZeroExitCode()
    {
        var result = await RunHelperAsync("fail", "7");

        Assert.Equal(7, result.ExitCode);
        Assert.False(result.Success);
    }

    [Fact]
    public async Task StdoutIsCaptured()
    {
        var result = await RunHelperAsync("stdout", "hello stdout");

        Assert.Contains("hello stdout", result.StdoutText);
        Assert.Contains("hello stdout", result.CombinedOutputText);
    }

    [Fact]
    public async Task StderrIsCaptured()
    {
        var result = await RunHelperAsync("stderr", "hello stderr");

        Assert.Contains("hello stderr", result.StderrText);
        Assert.Contains("hello stderr", result.CombinedOutputText);
    }

    [Fact]
    public async Task RealtimeOutputCallbackReceivesOutput()
    {
        var lines = new List<CommandOutputLine>();
        var result = await RunHelperAsync(lines.Add, "stdout", "callback output");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains(lines, line => line.Stream == CommandOutputStream.Stdout && line.Content == "callback output");
    }

    [Fact]
    public async Task WorkingDirectoryIsRespected()
    {
        using var directory = TempWorkingDirectory.Create();
        var result = await RunHelperAsync(new CommandRequest(
            "dotnet",
            HelperArguments("cwd"),
            WorkingDirectory: directory.Path));

        Assert.Equal(0, result.ExitCode);
        Assert.Contains(directory.Path, result.StdoutText);
    }

    [Fact]
    public async Task TimeoutCancelsLongRunningCommand()
    {
        var result = await RunHelperAsync(new CommandRequest(
            "dotnet",
            HelperArguments("sleep", "5000"),
            Timeout: TimeSpan.FromMilliseconds(250)));

        Assert.NotEqual(0, result.ExitCode);
        Assert.False(result.Success);
        Assert.True(result.Duration < TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task CommandArgumentsArePassedSafely()
    {
        var suspiciousArgument = "hello; exit 99";

        var result = await RunHelperAsync("args", suspiciousArgument);

        Assert.Equal(0, result.ExitCode);
        Assert.Contains($"ARG:{suspiciousArgument}", result.StdoutText);
    }

    private static Task<CommandResult> RunHelperAsync(params string[] helperArguments)
    {
        return RunHelperAsync(_ => { }, helperArguments);
    }

    private static Task<CommandResult> RunHelperAsync(Action<CommandOutputLine> onOutputLine, params string[] helperArguments)
    {
        return RunHelperAsync(new CommandRequest("dotnet", HelperArguments(helperArguments)), onOutputLine);
    }

    private static Task<CommandResult> RunHelperAsync(CommandRequest request)
    {
        return RunHelperAsync(request, _ => { });
    }

    private static Task<CommandResult> RunHelperAsync(CommandRequest request, Action<CommandOutputLine> onOutputLine)
    {
        var runner = new ProcessCommandRunner();
        return runner.RunAsync(request, onOutputLine, CancellationToken.None);
    }

    private static IReadOnlyList<string> HelperArguments(params string[] helperArguments)
    {
        return new[] { HelperDllPath() }.Concat(helperArguments).ToArray();
    }

    private static string HelperDllPath()
    {
        return Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "../../../../AgentForeman.CommandTestHelper/bin/Debug/net10.0/AgentForeman.CommandTestHelper.dll"));
    }
}
