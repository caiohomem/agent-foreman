namespace AgentForeman.Api;

public sealed record HealthResponseDto(string Status);

public sealed record DashboardSummaryResponseDto(
    int TotalMissions,
    int ActiveMissions,
    int PausedMissions,
    int FailedMissions,
    int ReviewMissions,
    int CompletedMissions);

public sealed record MissionResponseDto(
    string Id,
    string? ExternalWorkItemId,
    string? Source,
    string Title,
    string Status,
    string? Branch,
    string? PlanPath,
    string? PullRequestUrl,
    DateTimeOffset? RetryAfter,
    string? LastError,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? BlockedCommentPostedAt);

public sealed record MissionEventResponseDto(
    string Id,
    string MissionId,
    string? ExternalWorkItemId,
    string? RunId,
    string EventType,
    string Level,
    string Message,
    string? MetadataJson,
    DateTimeOffset CreatedAt);
