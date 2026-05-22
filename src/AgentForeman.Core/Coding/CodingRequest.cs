using AgentForeman.Core.Configuration;
using AgentForeman.Core.WorkItems;

namespace AgentForeman.Core.Coding;

public sealed record CodingRequest(
    string WorkItemId,
    string Title,
    string Body,
    IReadOnlyList<WorkItemLabel> Labels,
    string Repository,
    string RepoPath,
    string PlanPath,
    string PlanContent,
    string OutputDirectory,
    string? AgentsContent,
    string? PreviousLogs,
    string? CurrentDiff,
    AgentForemanConfig Config);
