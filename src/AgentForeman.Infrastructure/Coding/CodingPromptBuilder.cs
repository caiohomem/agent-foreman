using System.Text;
using AgentForeman.Core.Coding;
using AgentForeman.Core.Commands;
using AgentForeman.Core.Configuration;

namespace AgentForeman.Infrastructure.Coding;

internal static class CodingPromptBuilder
{
    public static string Build(CodingRequest request)
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

        if (request.Lessons is { Count: > 0 })
        {
            builder.AppendLine();
            builder.AppendLine("Lessons from previous runs (apply when relevant):");
            foreach (var lesson in request.Lessons)
                builder.AppendLine($"- {lesson.Title}: {Truncate(lesson.Body, 500)}");
        }

        if (!string.IsNullOrWhiteSpace(request.ResumeSummary))
        {
            builder.AppendLine();
            builder.AppendLine("Resume context from the previous run:");
            builder.AppendLine(request.ResumeSummary);
        }

        return builder.ToString();
    }

    private static string Truncate(string value, int max) => value.Length <= max ? value : value[..max];

    public static string BuildLog(CommandResult result)
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

    public static bool IsQuotaFailure(CommandResult result, AgentForemanConfig config)
    {
        var combined = $"{result.StdoutText}\n{result.StderrText}";
        return config.Quota.QuotaPatterns.Any(pattern =>
            !string.IsNullOrWhiteSpace(pattern)
            && combined.Contains(pattern, StringComparison.OrdinalIgnoreCase));
    }
}
