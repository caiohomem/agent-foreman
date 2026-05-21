namespace AgentForeman.Core.State;

public sealed record Mission(
    string Id,
    string? ExternalWorkItemId,
    string? Source,
    string Title,
    MissionStatus Status,
    string? Branch,
    string? PlanPath,
    string? PullRequestUrl,
    DateTimeOffset? RetryAfter,
    string? LastError,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
