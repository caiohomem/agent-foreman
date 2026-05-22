using AgentForeman.Core.Configuration;
using AgentForeman.Core.State;
using AgentForeman.Core.WorkItems;

namespace AgentForeman.Core.Orchestration;

public sealed record ResumeRequest(
    AgentForemanConfig Config,
    Mission Mission,
    WorkItem WorkItem,
    bool Force);
