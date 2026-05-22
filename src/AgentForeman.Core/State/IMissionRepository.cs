namespace AgentForeman.Core.State;

public interface IMissionRepository
{
    Mission? GetById(string id);
    void Save(Mission mission);
    IReadOnlyList<Mission> GetRecent(int limit);
    IReadOnlyList<Mission> GetByStatus(MissionStatus status, int limit);
}
