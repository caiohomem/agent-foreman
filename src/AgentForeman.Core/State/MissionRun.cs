namespace AgentForeman.Core.State;

public sealed record MissionRun(
    string Id,
    string MissionId,
    string Step,
    string Status,
    string? LogPath,
    DateTimeOffset StartedAt,
    DateTimeOffset? FinishedAt,
    int? ExitCode);
