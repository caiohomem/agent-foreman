namespace AgentForeman.Core.PullRequests;

public sealed record PullRequestResult(
    bool Success,
    string? PullRequestUrl,
    string Stdout,
    string Stderr,
    int ExitCode,
    DateTimeOffset StartedAt,
    DateTimeOffset FinishedAt,
    string? ErrorMessage);
