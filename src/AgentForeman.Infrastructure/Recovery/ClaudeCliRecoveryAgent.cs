using System.Text;
using System.Text.Json;
using AgentForeman.Core.Commands;
using AgentForeman.Core.Recovery;

namespace AgentForeman.Infrastructure.Recovery;

public sealed class ClaudeCliRecoveryAgent : IRecoveryAgent
{
    private readonly ICommandRunner _commandRunner;
    public ClaudeCliRecoveryAgent(ICommandRunner commandRunner) => _commandRunner = commandRunner;

    public async Task<RecoveryDiagnosis> DiagnoseAsync(RecoveryRequest request, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(request.OutputDirectory);
        var prompt = BuildPrompt(request);
        var model = string.IsNullOrWhiteSpace(request.Config.Recovery.Model) ? request.Config.Planner.Model : request.Config.Recovery.Model;
        var args = new List<string> { "--print" };
        if (!string.IsNullOrWhiteSpace(model)) { args.Add("--model"); args.Add(model); }
        args.Add(prompt);
        var result = await _commandRunner.RunAsync(new CommandRequest(request.Config.Planner.Command, args, WorkingDirectory: request.Config.Project.RepoPath), cancellationToken: cancellationToken);
        await File.WriteAllTextAsync(Path.Combine(request.OutputDirectory, $"recovery-{request.AttemptNumber}.log"),
            $"PROMPT:\n{prompt}\n\nSTDOUT:\n{result.StdoutText}\n\nSTDERR:\n{result.StderrText}", cancellationToken);
        return Parse(result.StdoutText);
    }

    public static RecoveryDiagnosis Parse(string json)
    {
        try
        {
            var value = JsonSerializer.Deserialize<DiagnosisJson>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return value is null || !Enum.TryParse<RecoveryCategory>(value.Category, true, out var category)
                ? Fallback("Recovery response was not valid JSON.")
                : new(category, value.Diagnosis ?? "", value.ProposedAction ?? "", value.LessonTitle ?? "", value.LessonBody ?? "", value.Confidence);
        }
        catch (JsonException ex) { return Fallback($"Recovery response parse failed: {ex.Message}"); }
    }

    private static RecoveryDiagnosis Fallback(string reason) => new(RecoveryCategory.NeedsHuman, reason, "Inspect the failure manually.", "Recovery diagnosis failed", reason, 0);

    private static string BuildPrompt(RecoveryRequest request)
    {
        var b = new StringBuilder();
        b.AppendLine("Classify this Agent Foreman failure. The caller executes only deterministic remediation.");
        b.AppendLine("Respond with ONLY a JSON object: {\"category\":\"DirtyWorktree|Transient|Quota|CodeError|ConfigError|NeedsHuman\",\"diagnosis\":\"...\",\"proposedAction\":\"...\",\"lessonTitle\":\"...\",\"lessonBody\":\"...\",\"confidence\":0.0}");
        b.AppendLine($"Stage: {request.FailedStage}\nError: {request.LastError}\nGit status:\n{request.GitStatusText}");
        b.AppendLine($"STDOUT tail:\n{Tail(request.Stdout)}\nSTDERR tail:\n{Tail(request.Stderr)}");
        if (request.SimilarLessons.Count > 0) b.AppendLine("Similar lessons:\n" + string.Join("\n", request.SimilarLessons.Select(x => $"- {x.Title}: {x.Body}")));
        return b.ToString();
    }

    private static string Tail(string value) => string.Join('\n', value.Split('\n').TakeLast(200));
    private sealed record DiagnosisJson(string? Category, string? Diagnosis, string? ProposedAction, string? LessonTitle, string? LessonBody, double Confidence);
}
