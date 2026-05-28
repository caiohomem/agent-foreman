namespace AgentForeman.Core.Summaries;

public sealed record RunSummaryResult(
    RunSummaryType SummaryType,
    string Content);
