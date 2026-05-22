using AgentForeman.Core.Configuration;

namespace AgentForeman.Core.Orchestration;

public sealed record RunOnceRequest(AgentForemanConfig Config);
