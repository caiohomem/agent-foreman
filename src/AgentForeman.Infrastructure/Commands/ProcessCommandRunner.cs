using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using AgentForeman.Core.Commands;

namespace AgentForeman.Infrastructure.Commands;

public sealed class ProcessCommandRunner : ICommandRunner
{
    public async Task<CommandResult> RunAsync(
        CommandRequest request,
        Action<CommandOutputLine>? onOutputLine = null,
        CancellationToken cancellationToken = default)
    {
        var startedAt = DateTimeOffset.UtcNow;
        var stdout = new StringBuilder();
        var stderr = new StringBuilder();
        var combined = new ConcurrentQueue<CommandOutputLine>();

        using var timeoutCts = request.Timeout is null
            ? null
            : new CancellationTokenSource(request.Timeout.Value);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            timeoutCts?.Token ?? CancellationToken.None);

        using var process = CreateProcess(request);
        var stdoutClosed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var stderrClosed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        if (request.CaptureStdout)
        {
            process.OutputDataReceived += (_, args) =>
            {
                if (args.Data is null)
                {
                    stdoutClosed.TrySetResult();
                    return;
                }

                AppendLine(CommandOutputStream.Stdout, args.Data, stdout, combined, onOutputLine);
            };
        }
        else
        {
            stdoutClosed.TrySetResult();
        }

        if (request.CaptureStderr)
        {
            process.ErrorDataReceived += (_, args) =>
            {
                if (args.Data is null)
                {
                    stderrClosed.TrySetResult();
                    return;
                }

                AppendLine(CommandOutputStream.Stderr, args.Data, stderr, combined, onOutputLine);
            };
        }
        else
        {
            stderrClosed.TrySetResult();
        }

        process.Start();

        if (request.CaptureStdout)
        {
            process.BeginOutputReadLine();
        }

        if (request.CaptureStderr)
        {
            process.BeginErrorReadLine();
        }

        var exitCode = await WaitForExitAsync(process, linkedCts.Token);
        await Task.WhenAll(stdoutClosed.Task, stderrClosed.Task);

        var finishedAt = DateTimeOffset.UtcNow;
        return new CommandResult(
            exitCode,
            stdout.ToString(),
            stderr.ToString(),
            string.Join(Environment.NewLine, combined.Select(line => line.Content)) + (combined.IsEmpty ? string.Empty : Environment.NewLine),
            startedAt,
            finishedAt);
    }

    private static Process CreateProcess(CommandRequest request)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = request.Executable,
            UseShellExecute = false,
            RedirectStandardOutput = request.CaptureStdout,
            RedirectStandardError = request.CaptureStderr,
        };

        if (!string.IsNullOrWhiteSpace(request.WorkingDirectory))
        {
            startInfo.WorkingDirectory = request.WorkingDirectory;
        }

        foreach (var argument in request.Arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        if (request.EnvironmentVariables is not null)
        {
            foreach (var variable in request.EnvironmentVariables)
            {
                startInfo.Environment[variable.Key] = variable.Value;
            }
        }

        return new Process { StartInfo = startInfo };
    }

    private static void AppendLine(
        CommandOutputStream stream,
        string content,
        StringBuilder target,
        ConcurrentQueue<CommandOutputLine> combined,
        Action<CommandOutputLine>? onOutputLine)
    {
        var line = new CommandOutputLine(stream, content, DateTimeOffset.UtcNow);

        lock (target)
        {
            target.AppendLine(content);
        }

        combined.Enqueue(line);
        onOutputLine?.Invoke(line);
    }

    private static async Task<int> WaitForExitAsync(Process process, CancellationToken cancellationToken)
    {
        try
        {
            await process.WaitForExitAsync(cancellationToken);
            return process.ExitCode;
        }
        catch (OperationCanceledException)
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync(CancellationToken.None);
            }

            return -1;
        }
    }
}
