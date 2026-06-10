using System.Text;
using AgentForeman.Core.Commands;
using AgentForeman.Core.Planning;

namespace AgentForeman.Infrastructure.Planning;

public sealed class ClaudeCliPlanningAgent : IPlanningAgent
{
    private readonly ICommandRunner _commandRunner;

    public ClaudeCliPlanningAgent(ICommandRunner commandRunner)
    {
        _commandRunner = commandRunner;
    }

    public async Task<PlanningResult> CreatePlanAsync(PlanningRequest request, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(request.OutputDirectory);

        var planPath = Path.Combine(request.OutputDirectory, "plan.md");
        var logPath = Path.Combine(request.OutputDirectory, "claude-plan.log");
        var prompt = BuildPrompt(request);
        var arguments = BuildArguments(request.Config.Planner.Model, prompt);
        var result = await _commandRunner.RunAsync(
            new CommandRequest(
                request.Config.Planner.Command,
                arguments,
                WorkingDirectory: request.RepoPath),
            cancellationToken: cancellationToken);

        await File.WriteAllTextAsync(planPath, result.StdoutText, cancellationToken);
        await File.WriteAllTextAsync(logPath, BuildLog(result), cancellationToken);

        return result.Success
            ? PlanningResult.Succeeded(planPath, logPath, result.StdoutText, result.StderrText, result.ExitCode, result.StartedAt, result.FinishedAt)
            : PlanningResult.Failure(
                planPath,
                logPath,
                result.StdoutText,
                result.StderrText,
                result.ExitCode,
                result.StartedAt,
                result.FinishedAt,
                BuildErrorMessage(result));
    }

    private static IReadOnlyList<string> BuildArguments(string model, string prompt)
    {
        var arguments = new List<string>(capacity: 3) { "--print" };
        if (!string.IsNullOrWhiteSpace(model))
        {
            arguments.Add("--model");
            arguments.Add(model);
        }
        arguments.Add(prompt);
        return arguments;
    }

    private static string BuildPrompt(PlanningRequest request)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Create a technical implementation plan for the following work item.");
        builder.AppendLine();
        builder.AppendLine("Instructions:");
        builder.AppendLine("- Read AGENTS.md if present.");
        builder.AppendLine("- Inspect the repository.");
        builder.AppendLine("- Create a technical implementation plan.");
        builder.AppendLine("- Do not modify files.");
        builder.AppendLine("- Do not run destructive commands.");
        builder.AppendLine("- Do not implement the issue.");
        builder.AppendLine("- Keep the plan small and incremental.");
        builder.AppendLine("- Include: summary, files likely to change, implementation steps, acceptance criteria, tests/build commands, risks, and out of scope.");
        builder.AppendLine();
        builder.AppendLine($"Repository: {request.Repository}");
        builder.AppendLine($"Repo path: {request.RepoPath}");
        builder.AppendLine($"Issue #{request.WorkItemId}: {request.Title}");
        builder.AppendLine($"Labels: {string.Join(", ", request.Labels.Select(label => label.Name))}");
        builder.AppendLine();
        builder.AppendLine("Body:");
        builder.AppendLine(request.Body);

        if (!string.IsNullOrWhiteSpace(request.AgentsContent))
        {
            builder.AppendLine();
            builder.AppendLine("AGENTS.md:");
            builder.AppendLine(request.AgentsContent);
        }

        AppendLessons(builder, request.Lessons);

        return builder.ToString();
    }

    private static void AppendLessons(StringBuilder builder, IReadOnlyList<AgentForeman.Core.State.Lesson>? lessons)
    {
        if (lessons is null || lessons.Count == 0) return;
        builder.AppendLine();
        builder.AppendLine("Lessons from previous runs (apply when relevant):");
        foreach (var lesson in lessons)
            builder.AppendLine($"- {lesson.Title}: {Truncate(lesson.Body, 500)}");
    }

    private static string BuildErrorMessage(CommandResult result)
    {
        var detail = !string.IsNullOrWhiteSpace(result.StderrText) ? result.StderrText.Trim() : result.StdoutText.Trim();
        return string.IsNullOrWhiteSpace(detail)
            ? $"Claude planner failed with exit code {result.ExitCode}."
            : $"Claude planner failed with exit code {result.ExitCode}: {Truncate(detail, 2000)}";
    }

    private static string Truncate(string value, int max) => value.Length <= max ? value : value[^max..];

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
