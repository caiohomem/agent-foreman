using AgentForeman.Core.Summaries;

namespace AgentForeman.Infrastructure.Summaries;

public static class RunSummaryPaths
{
    public static string GetPath(string outputDirectory, RunSummaryType summaryType)
    {
        var fileName = summaryType switch
        {
            RunSummaryType.SuccessSummary => "summary.md",
            RunSummaryType.FailureSummary => "failure-summary.md",
            RunSummaryType.ResumeContext => "resume-context.md",
            _ => throw new ArgumentOutOfRangeException(nameof(summaryType), summaryType, null),
        };

        return Path.Combine(outputDirectory, fileName);
    }
}
