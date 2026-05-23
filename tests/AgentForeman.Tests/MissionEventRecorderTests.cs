using AgentForeman.Core.Events;

namespace AgentForeman.Tests;

public sealed class MissionEventRecorderTests
{
    [Fact]
    public async Task AppendMissionEventAsyncPersistsEvent()
    {
        var recorder = new FakeMissionEventRecorder();
        var missionEvent = new MissionEvent(
            "evt-1",
            "github-42",
            "42",
            null,
            MissionEventType.PlanningStarted,
            MissionEventLevel.Info,
            "Creating technical plan.",
            null,
            DateTimeOffset.UtcNow);

        await recorder.AppendMissionEventAsync(missionEvent, CancellationToken.None);

        Assert.Single(recorder.Events);
        Assert.Equal("evt-1", recorder.Events[0].Id);
    }

    [Fact]
    public async Task GetMissionEventsAsyncReturnsEventsInChronologicalOrder()
    {
        var recorder = new FakeMissionEventRecorder();
        await recorder.AppendMissionEventAsync(new MissionEvent("evt-2", "github-42", "42", null, MissionEventType.ExecutionStarted, MissionEventLevel.Info, "Running Codex.", null, DateTimeOffset.Parse("2026-05-23T04:21:30Z")), CancellationToken.None);
        await recorder.AppendMissionEventAsync(new MissionEvent("evt-1", "github-42", "42", null, MissionEventType.PlanningStarted, MissionEventLevel.Info, "Creating technical plan.", null, DateTimeOffset.Parse("2026-05-23T04:20:00Z")), CancellationToken.None);

        var events = await recorder.GetMissionEventsAsync("github-42", 10, CancellationToken.None);

        Assert.Collection(
            events,
            first => Assert.Equal("evt-1", first.Id),
            second => Assert.Equal("evt-2", second.Id));
    }
}
