namespace AgentForeman.Core.State;

public enum MissionStatus
{
    New,
    BranchCreated,
    Planning,
    PlanReady,
    Coding,
    CodingCompleted,
    Testing,
    TestsPassed,
    TestsFailed,
    PullRequestCreated,
    PausedQuota,
    Failed,
    Cancelled,
}
