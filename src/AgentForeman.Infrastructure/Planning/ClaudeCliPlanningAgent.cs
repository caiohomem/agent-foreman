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
        var result = await _commandRunner.RunAsync(
            new CommandRequest(
                request.Config.Planner.Command,
                new[] { "--print", prompt },
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
                string.IsNullOrWhiteSpace(result.StderrText) ? "Claude planner failed." : result.StderrText.Trim());
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
