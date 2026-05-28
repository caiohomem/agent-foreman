using AgentForeman.Core.Summaries;

namespace AgentForeman.Core.State;

public sealed record RunSummary(
    string Id,
    string MissionId,
    string? ExternalWorkItemId,
    RunSummaryType SummaryType,
    string Content,
    string? Path,
    DateTimeOffset CreatedAt);
