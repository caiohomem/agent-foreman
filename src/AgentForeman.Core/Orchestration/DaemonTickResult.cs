namespace AgentForeman.Core.Orchestration;

public sealed record DaemonTickResult(
    bool ActiveMissionSkipped,
    bool NoReadyWork,
    string? Message,
    string? PullRequestUrl);
