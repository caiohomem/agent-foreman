namespace AgentForeman.Core.Configuration;

public interface IAgentForemanConfigLoader
{
    AgentForemanConfigLoadResult Load(string path);
}
