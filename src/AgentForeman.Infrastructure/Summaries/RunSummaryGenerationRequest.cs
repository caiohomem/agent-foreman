using AgentForeman.Core.State;
using AgentForeman.Core.Summaries;

namespace AgentForeman.Infrastructure.Summaries;

public sealed record RunSummaryGenerationRequest(
    Mission Mission,
    string RepoPath,
    string OutputDirectory,
    string IssueTitle,
    string IssueBody,
    MissionStatus FinalMissionStatus,
    IReadOnlyList<RunSummaryType> SummaryTypes,
    string? PullRequestUrl = null,
    string? CurrentGitDiff = null);
