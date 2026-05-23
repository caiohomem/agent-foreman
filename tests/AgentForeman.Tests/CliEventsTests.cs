using AgentForeman.Cli;
using AgentForeman.Core.Configuration;
using AgentForeman.Core.Events;

namespace AgentForeman.Tests;

public sealed class CliEventsTests
{
    [Fact]
    public void EventsCommandPrintsRecentEvents()
    {
        var services = EventsTestServices.Valid();
        services.Events.Events.Add(new MissionEvent(
            "evt-1",
            "github-42",
            "42",
            null,
            MissionEventType.MissionStarted,
            MissionEventLevel.Info,
            "Running work item #42",
            null,
            DateTimeOffset.Parse("2026-05-23T04:20:00Z")));

        var result = services.Execute(new[] { "events", "github-42" });

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Events for github-42", result.Output);
        Assert.Contains("MissionStarted - Running work item #42", result.Output);
    }

    [Fact]
    public void EventsCommandRespectsLimit()
    {
        var services = EventsTestServices.Valid();

        var result = services.Execute(new[] { "events", "github-42", "--limit", "100" });

        Assert.Equal(0, result.ExitCode);
        Assert.Equal("github-42", services.Events.LastMissionId);
        Assert.Equal(100, services.Events.LastLimit);
    }
}

internal sealed class EventsTestServices
{
    private readonly FakeConfigLoader _configLoader;

    private EventsTestServices(FakeConfigLoader configLoader, FakeMissionEventRecorder events)
    {
        _configLoader = configLoader;
        Events = events;
    }

    public FakeMissionEventRecorder Events { get; }

    public static EventsTestServices Valid()
    {
        var config = new AgentForemanConfig
        {
            Database = new DatabaseConfig
            {
                Provider = "postgresql",
                ConnectionString = "Host=localhost;Database=agent_foreman",
            },
        };

        return new EventsTestServices(
            new FakeConfigLoader(AgentForemanConfigLoadResult.Success(config)),
            new FakeMissionEventRecorder());
    }

    public CommandResult Execute(IReadOnlyList<string> args)
    {
        var doctorServices = DoctorTestServices.Valid();
        return CliApplication.Execute(
            args,
            _configLoader,
            doctorServices.RepositoryChecker,
            doctorServices.CommandChecker,
            new FakeStateStore(),
            new RecordingCommandRunner(),
            missionEventRecorder: Events);
    }
}
