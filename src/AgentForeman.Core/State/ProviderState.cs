namespace AgentForeman.Core.State;

public sealed record ProviderState(
    string Provider,
    ProviderStatus Status,
    DateTimeOffset? LastSuccessAt,
    DateTimeOffset? LastLimitAt,
    DateTimeOffset? RetryAfter,
    int ConsecutiveFailures,
    DateTimeOffset UpdatedAt);
