namespace AgentForeman.Core.Events;

public sealed record MissionEvent(
    string Id,
    string MissionId,
    string? ExternalWorkItemId,
    string? RunId,
    MissionEventType EventType,
    MissionEventLevel Level,
    string Message,
    string? MetadataJson,
    DateTimeOffset CreatedAt);
