using System.Text;
using AgentForeman.Core.Commands;
using AgentForeman.Core.Summaries;

namespace AgentForeman.Infrastructure.Summaries;

public sealed class ClaudeCliRunSummaryGenerator : IRunSummaryGenerator
{
    private readonly ICommandRunner _commandRunner;
    private readonly string _command;
    private readonly string _model;

    public ClaudeCliRunSummaryGenerator(ICommandRunner commandRunner, string command = "claude", string model = "")
    {
        _commandRunner = commandRunner;
        _command = command;
        _model = model;
    }

    public async Task<RunSummaryResult> GenerateAsync(
        RunSummaryType summaryType,
        RunSummaryRequest request,
        CancellationToken cancellationToken)
    {
        var promptFilePath = WritePromptFile(summaryType, request);
        var prompt = BuildPrompt(summaryType, promptFilePath);
        var arguments = BuildArguments(prompt);
        var result = await _commandRunner.RunAsync(
            new CommandRequest(
                _command,
                arguments,
                WorkingDirectory: request.RepoPath),
            cancellationToken: cancellationToken);

        if (!result.Success)
        {
            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(result.StderrText)
                    ? "Claude run summary generation failed."
                    : result.StderrText.Trim());
        }

        return new RunSummaryResult(summaryType, result.StdoutText);
    }

    private IReadOnlyList<string> BuildArguments(string prompt)
    {
        var arguments = new List<string>(capacity: 3) { "--print" };
        if (!string.IsNullOrWhiteSpace(_model))
        {
            arguments.Add("--model");
            arguments.Add(_model);
        }
        arguments.Add(prompt);
        return arguments;
    }

    private static string BuildPrompt(RunSummaryType summaryType, string promptFilePath)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Generate a concise mission run summary for future retrieval and resume context.");
        builder.AppendLine();
        builder.AppendLine($"Read the full mission context from this file first: {promptFilePath}");
        builder.AppendLine("- Use only the information found in that file.");
        builder.AppendLine("Output requirements:");
        builder.AppendLine("- Use Markdown.");
        builder.AppendLine("- Be concise but specific.");
        builder.AppendLine("- Only describe what is supported by the provided artifacts.");
        builder.AppendLine("- If information is missing, say so briefly.");
        builder.AppendLine("- Always include these sections with exactly these headings:");
        builder.AppendLine("  - What changed");
        builder.AppendLine("  - Important decisions/patterns");
        builder.AppendLine("  - Errors encountered and fixes");
        builder.AppendLine("  - Files touched");
        builder.AppendLine("  - Tests/build results");
        builder.AppendLine("  - Follow-up risks");
        builder.AppendLine("  - Resume instructions if the mission needs to continue");

        return builder.ToString();
    }

    private static string WritePromptFile(RunSummaryType summaryType, RunSummaryRequest request)
    {
        Directory.CreateDirectory(request.RunDirectory);
        var path = Path.Combine(
            request.RunDirectory,
            $"summary-context-{summaryType.ToString().ToLowerInvariant()}.md");

        var builder = new StringBuilder();
        builder.AppendLine($"Summary type: {summaryType}");
        builder.AppendLine($"Mission id: {request.MissionId}");
        builder.AppendLine($"External work item id: {request.ExternalWorkItemId}");
        builder.AppendLine($"Issue title: {request.IssueTitle}");
        builder.AppendLine($"Final mission status: {request.FinalMissionStatus}");
        if (!string.IsNullOrWhiteSpace(request.PullRequestUrl))
        {
            builder.AppendLine($"PR URL: {request.PullRequestUrl}");
        }

        builder.AppendLine();
        builder.AppendLine("Issue body:");
        builder.AppendLine(request.IssueBody);

        AppendSection(builder, "plan.md content", request.PlanContent);
        AppendSection(builder, "codex-exec.log content", request.ExecutionLogContent);
        AppendSection(builder, "tests.log content", request.TestsLogContent);

        if (request.RepairLogs.Count > 0)
        {
            foreach (var repairLog in request.RepairLogs)
            {
                builder.AppendLine();
                builder.AppendLine($"Repair log: {repairLog.Path}");
                builder.AppendLine(repairLog.Content);
            }
        }

        AppendSection(builder, "current git diff", request.CurrentGitDiff);
        File.WriteAllText(path, builder.ToString());
        return path;
    }

    private static void AppendSection(StringBuilder builder, string label, string? content)
    {
        builder.AppendLine();
        builder.AppendLine($"{label}:");
        builder.AppendLine(string.IsNullOrWhiteSpace(content) ? "(not available)" : content);
    }
}
