namespace AgentForeman.Core.PullRequests;

public sealed record PullRequestRequest(
    string WorkItemId,
    string Title,
    string Body,
    string Repository,
    string RepoPath,
    string Branch,
    string BaseBranch,
    string CommitMessage,
    string PullRequestTitle,
    string PullRequestBody);
