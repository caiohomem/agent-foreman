using System.Text;
using AgentForeman.Core.Coding;
using AgentForeman.Core.Commands;
using AgentForeman.Core.Configuration;

namespace AgentForeman.Infrastructure.Coding;

public sealed class CodexCliCodingAgent : ICodingAgent
{
    private readonly ICommandRunner _commandRunner;

    public CodexCliCodingAgent(ICommandRunner commandRunner)
    {
        _commandRunner = commandRunner;
    }

    public async Task<CodingResult> ExecuteAsync(CodingRequest request, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(request.OutputDirectory);

        var logPath = Path.Combine(request.OutputDirectory, "codex-exec.log");
        var prompt = BuildPrompt(request);
        var arguments = new List<string>
        {
            "--ask-for-approval",
            request.Config.Executor.Approval,
            "exec",
            "--sandbox",
            request.Config.Executor.Sandbox,
            "--cd",
            request.RepoPath,
            prompt,
        };

        var result = await _commandRunner.RunAsync(
            new CommandRequest(
                request.Config.Executor.Command,
                arguments,
                WorkingDirectory: request.RepoPath),
            cancellationToken: cancellationToken);

        await File.WriteAllTextAsync(logPath, BuildLog(result), cancellationToken);

        var quotaDetected = IsQuotaFailure(result, request.Config);

        if (result.Success)
        {
            return CodingResult.Succeeded(logPath, result.StdoutText, result.StderrText, result.ExitCode, result.StartedAt, result.FinishedAt);
        }

        var error = quotaDetected
            ? "Codex CLI reported a quota or rate limit."
            : (string.IsNullOrWhiteSpace(result.StderrText) ? "Codex executor failed." : result.StderrText.Trim());

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

    private static bool IsQuotaFailure(CommandResult result, AgentForemanConfig config)
    {
        var combined = $"{result.StdoutText}\n{result.StderrText}";
        return config.Quota.QuotaPatterns.Any(pattern =>
            !string.IsNullOrWhiteSpace(pattern)
            && combined.Contains(pattern, StringComparison.OrdinalIgnoreCase));
    }

    private static string BuildPrompt(CodingRequest request)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Implement the work item described below using the saved plan.");
        builder.AppendLine();
        builder.AppendLine("Instructions:");
        builder.AppendLine("- Read AGENTS.md first if present.");
        builder.AppendLine("- Follow the saved plan at " + request.PlanPath + ".");
        builder.AppendLine("- Implement only the specified work item.");
        builder.AppendLine("- Do not implement unrelated features.");
        builder.AppendLine("- Do not commit changes.");
        builder.AppendLine("- Do not push.");
        builder.AppendLine("- Do not create pull requests.");
        builder.AppendLine("- Do not modify secrets, .env files, or production configuration.");
        builder.AppendLine("- Preserve existing work in the repository.");
        builder.AppendLine("- Run relevant local checks only if useful for verifying the change.");
        builder.AppendLine("- Stop and report back if the task is ambiguous.");
        builder.AppendLine();
        builder.AppendLine($"Repository: {request.Repository}");
        builder.AppendLine($"Repo path: {request.RepoPath}");
        builder.AppendLine($"Issue #{request.WorkItemId}: {request.Title}");
        builder.AppendLine($"Labels: {string.Join(", ", request.Labels.Select(label => label.Name))}");
        builder.AppendLine();
        builder.AppendLine("Body:");
        builder.AppendLine(request.Body);
        builder.AppendLine();
        builder.AppendLine("Plan:");
        builder.AppendLine(request.PlanContent);

        if (!string.IsNullOrWhiteSpace(request.AgentsContent))
        {
            builder.AppendLine();
            builder.AppendLine("AGENTS.md:");
            builder.AppendLine(request.AgentsContent);
        }

        if (!string.IsNullOrWhiteSpace(request.PreviousLogs))
        {
            builder.AppendLine();
            builder.AppendLine("Previous logs:");
            builder.AppendLine(request.PreviousLogs);
        }

        if (!string.IsNullOrWhiteSpace(request.CurrentDiff))
        {
            builder.AppendLine();
            builder.AppendLine("Current git diff:");
            builder.AppendLine(request.CurrentDiff);
        }

        return builder.ToString();
    }

    private static string BuildLog(CommandResult result)
    {
        return $"""
            Exit code: {result.ExitCode}
            Started at: {result.StartedAt:O}
            Finished at: {result.FinishedAt:O}

            STDOUT:
            {result.StdoutText}

            STDERR:
            {result.StderrText}
            """;
    }
}
