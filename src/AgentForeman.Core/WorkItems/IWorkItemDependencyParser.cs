namespace AgentForeman.Core.WorkItems;

public interface IWorkItemDependencyParser
{
    IReadOnlyList<WorkItemDependency> Parse(string body, string repository);
}

