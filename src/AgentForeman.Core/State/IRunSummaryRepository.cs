using AgentForeman.Core.Summaries;

namespace AgentForeman.Core.State;

public interface IRunSummaryRepository
{
    Task SaveRunSummaryAsync(RunSummary runSummary, CancellationToken cancellationToken);
    Task<IReadOnlyList<RunSummary>> GetRunSummariesAsync(string missionId, CancellationToken cancellationToken);
    Task<RunSummary?> GetLatestRunSummaryAsync(string missionId, RunSummaryType? summaryType, CancellationToken cancellationToken);
}
