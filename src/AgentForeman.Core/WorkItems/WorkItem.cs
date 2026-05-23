namespace AgentForeman.Core.WorkItems;

public sealed record WorkItem(
    string ExternalId,
    WorkItemSource Source,
    string Title,
    string Body,
    string Url,
    string Repository,
    IReadOnlyList<WorkItemLabel> Labels,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<WorkItemDependency>? Dependencies = null,
    WorkItemState State = WorkItemState.Open,
    DateTimeOffset? ClosedAt = null);
