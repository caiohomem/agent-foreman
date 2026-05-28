namespace AgentForeman.Core.Summaries;

public interface IRunSummaryGenerator
{
    Task<RunSummaryResult> GenerateAsync(
        RunSummaryType summaryType,
        RunSummaryRequest request,
        CancellationToken cancellationToken);
}
