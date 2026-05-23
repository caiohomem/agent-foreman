namespace AgentForeman.Core.Events;

public interface IMissionEventRecorder
{
    Task AppendMissionEventAsync(MissionEvent missionEvent, CancellationToken cancellationToken);
    Task<IReadOnlyList<MissionEvent>> GetMissionEventsAsync(string missionId, int limit, CancellationToken cancellationToken);
    Task<IReadOnlyList<MissionEvent>> GetRecentMissionEventsAsync(int limit, CancellationToken cancellationToken);
}
