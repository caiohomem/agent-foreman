namespace AgentForeman.Core.State;

public interface IProviderStateRepository
{
    ProviderState? Get(string provider);
    void Save(ProviderState state);
}
