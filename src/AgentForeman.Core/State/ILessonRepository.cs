namespace AgentForeman.Core.State;

public interface ILessonRepository
{
    Task SaveAsync(Lesson lesson, CancellationToken cancellationToken);
    Task<IReadOnlyList<Lesson>> SearchAsync(string query, int topK, CancellationToken cancellationToken);
    Task<IReadOnlyList<Lesson>> GetRecentAsync(int topK, CancellationToken cancellationToken);
}
