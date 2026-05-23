namespace AgentForeman.Core.WorkItems;

public sealed record WorkItemDependency(
    string Reference,
    string Repository,
    WorkItemDependencyStatus Status = WorkItemDependencyStatus.Unknown);

