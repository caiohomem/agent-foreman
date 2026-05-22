using AgentForeman.Core.State;

namespace AgentForeman.Core.Orchestration;

public sealed record ResumeResult(
    bool Success,
    string? PullRequestUrl,
    MissionStatus? FinalStatus,
    string? ErrorMessage,
    bool QuotaDetected,
    DateTimeOffset? RetryAfter);
