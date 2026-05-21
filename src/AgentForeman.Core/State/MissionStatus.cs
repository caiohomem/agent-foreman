namespace AgentForeman.Core.State;

public enum MissionStatus
{
    New,
    BranchCreated,
    Planning,
    PlanReady,
    Coding,
    Testing,
    PullRequestCreated,
    PausedQuota,
    Failed,
    Cancelled,
}
