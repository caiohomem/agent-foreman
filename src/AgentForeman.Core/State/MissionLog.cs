namespace AgentForeman.Core.State;

public sealed record MissionLog(
    long Id,
    string MissionId,
    string? RunId,
    int Sequence,
    string Stream,
    string Content,
    DateTimeOffset CreatedAt);
