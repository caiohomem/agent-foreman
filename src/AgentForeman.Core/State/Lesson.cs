namespace AgentForeman.Core.State;

public sealed record Lesson(
    string Id,
    string? MissionId,
    string? ExternalWorkItemId,
    string Category,
    string Title,
    string Body,
    string Outcome,
    string Source,
    DateTimeOffset CreatedAt);
