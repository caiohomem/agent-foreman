using AgentForeman.Core.Configuration;
using AgentForeman.Core.WorkItems;

namespace AgentForeman.Core.Planning;

public sealed record PlanningRequest(
    string WorkItemId,
    string Title,
    string Body,
    IReadOnlyList<WorkItemLabel> Labels,
    string Repository,
    string RepoPath,
    string OutputDirectory,
    string? AgentsContent,
    AgentForemanConfig Config);

