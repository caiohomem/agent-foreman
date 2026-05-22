namespace AgentForeman.Core.Git;

public sealed record BranchPreparationRequest(
    string RepoPath,
    string DefaultBranch,
    string MissionBranch);
