namespace AgentForeman.Core.Planning;

public sealed record PlanningResult(
    bool Success,
    string PlanPath,
    string LogPath,
    string Stdout,
    string Stderr,
    int ExitCode,
    DateTimeOffset StartedAt,
    DateTimeOffset FinishedAt,
    string? ErrorMessage)
{
    public static PlanningResult Succeeded(
        string planPath,
        string logPath,
        string stdout,
        string stderr,
        int exitCode,
        DateTimeOffset startedAt,
        DateTimeOffset finishedAt)
    {
        return new PlanningResult(true, planPath, logPath, stdout, stderr, exitCode, startedAt, finishedAt, null);
    }

    public static PlanningResult Failure(
        string PlanPath,
        string LogPath,
        string Stdout,
        string Stderr,
        int ExitCode,
        DateTimeOffset StartedAt,
        DateTimeOffset FinishedAt,
        string ErrorMessage)
    {
        return new PlanningResult(false, PlanPath, LogPath, Stdout, Stderr, ExitCode, StartedAt, FinishedAt, ErrorMessage);
    }
}
