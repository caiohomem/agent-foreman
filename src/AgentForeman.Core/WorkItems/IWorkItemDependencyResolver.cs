namespace AgentForeman.Core.WorkItems;

public interface IWorkItemDependencyResolver
{
    Task<WorkItemDependency> ResolveAsync(WorkItemDependency dependency, CancellationToken cancellationToken);
}

