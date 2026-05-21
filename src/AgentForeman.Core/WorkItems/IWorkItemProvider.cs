namespace AgentForeman.Core.WorkItems;

public interface IWorkItemProvider
{
    Task<IReadOnlyList<WorkItem>> GetReadyItemsAsync(CancellationToken cancellationToken);
    Task<WorkItem> GetWorkItemAsync(string externalId, CancellationToken cancellationToken);
    Task MarkAsWorkingAsync(WorkItem item, CancellationToken cancellationToken);
    Task MarkAsPausedAsync(WorkItem item, string reason, DateTimeOffset retryAfter, CancellationToken cancellationToken);
    Task MarkAsReviewAsync(WorkItem item, string pullRequestUrl, CancellationToken cancellationToken);
    Task AddCommentAsync(WorkItem item, string comment, CancellationToken cancellationToken);
    Task<WorkItem> CreateWorkItemAsync(CreateWorkItemRequest request, CancellationToken cancellationToken);
}
