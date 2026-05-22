using AgentForeman.Core.Configuration;

namespace AgentForeman.Core.Orchestration;

public sealed record DaemonTickRequest(AgentForemanConfig Config);
