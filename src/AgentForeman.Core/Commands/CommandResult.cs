namespace AgentForeman.Core.Commands;

public sealed record CommandResult(
    int ExitCode,
    string StdoutText,
    string StderrText,
    string CombinedOutputText,
    DateTimeOffset StartedAt,
    DateTimeOffset FinishedAt)
{
    public TimeSpan Duration => FinishedAt - StartedAt;
    public bool Success => ExitCode == 0;
}
