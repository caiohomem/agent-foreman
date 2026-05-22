using System.Text;
using AgentForeman.Core.Commands;
using AgentForeman.Core.Testing;

namespace AgentForeman.Infrastructure.Testing;

public sealed class ConfiguredCommandTestRunner : ITestRunner
{
    private readonly ICommandRunner _commandRunner;

    public ConfiguredCommandTestRunner(ICommandRunner commandRunner)
    {
        _commandRunner = commandRunner;
    }

    public async Task<TestRunResult> RunAsync(TestRunRequest request, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(request.OutputDirectory);

        var logPath = Path.Combine(request.OutputDirectory, "tests.log");
        var startedAt = DateTimeOffset.UtcNow;
        var commandResults = new List<TestCommandResult>();
        var log = new StringBuilder();

        foreach (var command in request.Commands)
        {
            var (executable, arguments) = ParseCommand(command);
            var cmdStart = DateTimeOffset.UtcNow;
            var result = await _commandRunner.RunAsync(
                new CommandRequest(executable, arguments, WorkingDirectory: request.RepoPath),
                cancellationToken: cancellationToken);

            var cmdResult = new TestCommandResult(
                command,
                result.ExitCode,
                result.StdoutText,
                result.StderrText,
                result.Duration);

            commandResults.Add(cmdResult);

            log.AppendLine($"$ {command}");
            log.AppendLine($"Exit code: {result.ExitCode}");
            if (!string.IsNullOrWhiteSpace(result.StdoutText))
            {
                log.AppendLine("STDOUT:");
                log.AppendLine(result.StdoutText);
            }
            if (!string.IsNullOrWhiteSpace(result.StderrText))
            {
                log.AppendLine("STDERR:");
                log.AppendLine(result.StderrText);
            }
            log.AppendLine();

            if (!cmdResult.Success)
            {
                var finishedAt = DateTimeOffset.UtcNow;
                await File.WriteAllTextAsync(logPath, log.ToString(), cancellationToken);
                return new TestRunResult(
                    false,
                    logPath,
                    commandResults,
                    startedAt,
                    finishedAt,
                    $"Command failed (exit {result.ExitCode}): {command}");
            }
        }

        var finishedAt2 = DateTimeOffset.UtcNow;
        await File.WriteAllTextAsync(logPath, log.ToString(), cancellationToken);
        return new TestRunResult(true, logPath, commandResults, startedAt, finishedAt2, null);
    }

    public static (string Executable, IReadOnlyList<string> Arguments) ParseCommand(string commandLine)
    {
        var tokens = Tokenize(commandLine);
        if (tokens.Count == 0)
            throw new ArgumentException($"Empty command: {commandLine}");
        return (tokens[0], tokens.Skip(1).ToArray());
    }

    private static IReadOnlyList<string> Tokenize(string input)
    {
        var tokens = new List<string>();
        var current = new StringBuilder();
        var inQuote = false;
        var quoteChar = '"';

        foreach (var c in input)
        {
            if (inQuote)
            {
                if (c == quoteChar)
                    inQuote = false;
                else
                    current.Append(c);
            }
            else if (c == '"' || c == '\'')
            {
                inQuote = true;
                quoteChar = c;
            }
            else if (c == ' ')
            {
                if (current.Length > 0)
                {
                    tokens.Add(current.ToString());
                    current.Clear();
                }
            }
            else
            {
                current.Append(c);
            }
        }

        if (current.Length > 0)
            tokens.Add(current.ToString());

        return tokens;
    }
}
