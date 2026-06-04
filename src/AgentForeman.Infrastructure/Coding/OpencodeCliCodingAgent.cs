using AgentForeman.Core.Coding;
using AgentForeman.Core.Commands;

namespace AgentForeman.Infrastructure.Coding;

public sealed class OpencodeCliCodingAgent : ICodingAgent
{
    public const string DefaultModel = "opencode/minimax-m3-free";

    private readonly ICommandRunner _commandRunner;

    public OpencodeCliCodingAgent(ICommandRunner commandRunner)
    {
        _commandRunner = commandRunner;
    }

    public async Task<CodingResult> ExecuteAsync(CodingRequest request, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(request.OutputDirectory);

        var logPath = Path.Combine(request.OutputDirectory, "opencode-exec.log");
        var prompt = CodingPromptBuilder.Build(request);
        var model = string.IsNullOrWhiteSpace(request.Config.Executor.Model)
            ? DefaultModel
            : request.Config.Executor.Model;

        var arguments = new List<string>
        {
            "run",
            "--model",
            model,
            "--dir",
            request.RepoPath,
            "--dangerously-skip-permissions",
            prompt,
        };

        var result = await _commandRunner.RunAsync(
            new CommandRequest(
                request.Config.Executor.Command,
                arguments,
                WorkingDirectory: request.RepoPath),
            cancellationToken: cancellationToken);

        await File.WriteAllTextAsync(logPath, CodingPromptBuilder.BuildLog(result), cancellationToken);

        var quotaDetected = CodingPromptBuilder.IsQuotaFailure(result, request.Config);

        if (result.Success)
        {
            return CodingResult.Succeeded(logPath, result.StdoutText, result.StderrText, result.ExitCode, result.StartedAt, result.FinishedAt);
        }

        var error = quotaDetected
            ? "Opencode CLI reported a quota or rate limit."
            : (string.IsNullOrWhiteSpace(result.StderrText) ? "Opencode executor failed." : result.StderrText.Trim());

        return CodingResult.Failure(
            logPath,
            result.StdoutText,
            result.StderrText,
            result.ExitCode,
            result.StartedAt,
            result.FinishedAt,
            error,
            quotaDetected);
    }
}
