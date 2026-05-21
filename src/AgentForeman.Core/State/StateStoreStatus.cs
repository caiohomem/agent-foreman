using AgentForeman.Core.Configuration;

namespace AgentForeman.Core.State;

public sealed record StateStoreStatus(string Provider, int MissionCount, int ProviderStateCount);

public interface IStateStore
{
    void Initialize(AgentForemanConfig config);
    StateStoreStatus GetStatus(AgentForemanConfig config);
}
