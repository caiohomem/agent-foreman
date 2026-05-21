namespace AgentForeman.Core.WorkItems;

public sealed record CreateWorkItemRequest(
    string Title,
    string Body,
    IReadOnlyList<string> Labels);
