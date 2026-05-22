namespace AgentForeman.Core.Coding;

public sealed record CodingResult(
    bool Success,
    string LogPath,
    string Stdout,
    string Stderr,
    int ExitCode,
    DateTimeOffset StartedAt,
    DateTimeOffset FinishedAt,
    string? ErrorMessage,
    bool QuotaDetected)
{
    public static CodingResult Succeeded(
        string logPath,
        string stdout,
        string stderr,
        int exitCode,
        DateTimeOffset startedAt,
        DateTimeOffset finishedAt)
    {
        return new CodingResult(true, logPath, stdout, stderr, exitCode, startedAt, finishedAt, null, false);
    }

    public static CodingResult Failure(
        string logPath,
        string stdout,
        string stderr,
        int exitCode,
        DateTimeOffset startedAt,
        DateTimeOffset finishedAt,
        string errorMessage,
        bool quotaDetected = false)
    {
        return new CodingResult(false, logPath, stdout, stderr, exitCode, startedAt, finishedAt, errorMessage, quotaDetected);
    }
}
