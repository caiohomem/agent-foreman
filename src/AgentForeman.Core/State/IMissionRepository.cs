namespace AgentForeman.Core.State;

public interface IMissionRepository
{
    Mission? GetById(string id);
    void Save(Mission mission);
}
