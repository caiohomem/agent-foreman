using AgentForeman.Core.Configuration;
using AgentForeman.Core.State;
using AgentForeman.Core.Summaries;

namespace AgentForeman.Infrastructure.Memory;

public sealed class MemoryContextProvider
{
    private readonly ILessonRepository _lessons;
    private readonly IRunSummaryRepository _summaries;

    public MemoryContextProvider(ILessonRepository lessons, IRunSummaryRepository summaries)
    {
        _lessons = lessons;
        _summaries = summaries;
    }

    public async Task<IReadOnlyList<Lesson>> GetLessonsAsync(string query, AgentForemanConfig config, CancellationToken cancellationToken)
    {
        if (!config.Memory.Enabled) return Array.Empty<Lesson>();
        return await _lessons.SearchAsync(query, config.Memory.TopKLessons, cancellationToken);
    }

    public async Task<string?> GetResumeContextAsync(string missionId, AgentForemanConfig config, CancellationToken cancellationToken)
    {
        if (!config.Memory.Enabled || !config.Memory.InjectResumeContext) return null;
        return (await _summaries.GetLatestRunSummaryAsync(missionId, RunSummaryType.ResumeContext, cancellationToken))?.Content;
    }
}
