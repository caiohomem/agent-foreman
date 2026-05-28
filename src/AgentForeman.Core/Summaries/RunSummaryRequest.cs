namespace AgentForeman.Core.Summaries;

public sealed record RunSummaryRequest(
    string MissionId,
    string? ExternalWorkItemId,
    string RunDirectory,
    string IssueTitle,
    string IssueBody,
    string? PlanContent,
    string? ExecutionLogContent,
    string? TestsLogContent,
    IReadOnlyList<RunSummaryArtifact> RepairLogs,
    string? CurrentGitDiff,
    string? PullRequestUrl,
    string FinalMissionStatus);
