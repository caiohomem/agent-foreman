using AgentForeman.Core.State;

namespace AgentForeman.Core.Orchestration;

public sealed record RunOnceResult(
    bool Success,
    string? PullRequestUrl,
    string? WorkItemId,
    string? WorkItemTitle,
    MissionStatus? FinalStatus,
    string? ErrorMessage,
    bool NoReadyWorkItems,
    bool QuotaDetected,
    DateTimeOffset? RetryAfter);
