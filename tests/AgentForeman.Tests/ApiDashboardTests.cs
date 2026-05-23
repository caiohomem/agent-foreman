using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AgentForeman.Api;
using AgentForeman.Core.Events;
using AgentForeman.Core.State;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AgentForeman.Tests;

public sealed class ApiDashboardTests
{
    [Fact]
    public async Task HealthEndpointReturnsOk()
    {
        using var client = CreateClient();

        var response = await client.GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("ok", payload.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public async Task DashboardSummaryReturnsCounts()
    {
        var missions = new FakeMissionRepository();
        SeedMission(missions, "m-1", MissionStatus.Planning, 1);
        SeedMission(missions, "m-2", MissionStatus.PausedQuota, 2);
        SeedMission(missions, "m-3", MissionStatus.Failed, 3);
        SeedMission(missions, "m-4", MissionStatus.TestsFailed, 4);
        SeedMission(missions, "m-5", MissionStatus.PullRequestCreated, 5);
        SeedMission(missions, "m-6", MissionStatus.Completed, 6);
        SeedMission(missions, "m-7", MissionStatus.Cancelled, 7);

        using var client = CreateClient(missions: missions);

        var response = await client.GetAsync("/api/dashboard/summary");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(7, payload.RootElement.GetProperty("totalMissions").GetInt32());
        Assert.Equal(1, payload.RootElement.GetProperty("activeMissions").GetInt32());
        Assert.Equal(1, payload.RootElement.GetProperty("pausedMissions").GetInt32());
        Assert.Equal(2, payload.RootElement.GetProperty("failedMissions").GetInt32());
        Assert.Equal(1, payload.RootElement.GetProperty("reviewMissions").GetInt32());
        Assert.Equal(1, payload.RootElement.GetProperty("completedMissions").GetInt32());
    }

    [Fact]
    public async Task MissionsEndpointReturnsMissionList()
    {
        var missions = new FakeMissionRepository();
        SeedMission(missions, "old", MissionStatus.Coding, 2);
        SeedMission(missions, "new", MissionStatus.PullRequestCreated, 1);

        using var client = CreateClient(missions: missions);

        var response = await client.GetAsync("/api/missions");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("new", payload[0].GetProperty("id").GetString());
        Assert.Equal("old", payload[1].GetProperty("id").GetString());
    }

    [Fact]
    public async Task MissionDetailsReturns404WhenNotFound()
    {
        using var client = CreateClient();

        var response = await client.GetAsync("/api/missions/missing");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task MissionEventsEndpointReturnsEvents()
    {
        var events = new FakeMissionEventRecorder();
        events.Events.Add(new MissionEvent("e-1", "m-1", "42", null, MissionEventType.PlanningStarted, MissionEventLevel.Info, "Planning", null, DateTimeOffset.UtcNow.AddMinutes(-2)));
        events.Events.Add(new MissionEvent("e-2", "m-1", "42", null, MissionEventType.PlanningCompleted, MissionEventLevel.Info, "Done", "{\"step\":1}", DateTimeOffset.UtcNow.AddMinutes(-1)));

        using var client = CreateClient(events: events);

        var response = await client.GetAsync("/api/missions/m-1/events");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, payload.GetArrayLength());
        Assert.Equal("e-1", payload[0].GetProperty("id").GetString());
        Assert.Equal("e-2", payload[1].GetProperty("id").GetString());
    }

    [Fact]
    public async Task StatusFilterWorks()
    {
        var missions = new FakeMissionRepository();
        SeedMission(missions, "failed", MissionStatus.Failed, 1);
        SeedMission(missions, "active", MissionStatus.Coding, 2);

        using var client = CreateClient(missions: missions);

        var response = await client.GetAsync("/api/missions?status=Failed");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Single(payload.EnumerateArray());
        Assert.Equal("failed", payload[0].GetProperty("id").GetString());
    }

    [Fact]
    public async Task LimitParameterWorks()
    {
        var missions = new FakeMissionRepository();
        SeedMission(missions, "first", MissionStatus.New, 2);
        SeedMission(missions, "second", MissionStatus.New, 1);
        var events = new FakeMissionEventRecorder();
        events.Events.Add(new MissionEvent("e-1", "m-1", null, null, MissionEventType.PlanningStarted, MissionEventLevel.Info, "first", null, DateTimeOffset.UtcNow.AddMinutes(-2)));
        events.Events.Add(new MissionEvent("e-2", "m-1", null, null, MissionEventType.PlanningCompleted, MissionEventLevel.Info, "second", null, DateTimeOffset.UtcNow.AddMinutes(-1)));

        using var client = CreateClient(missions, events);

        var missionsResponse = await client.GetAsync("/api/missions?limit=1");
        var eventsResponse = await client.GetAsync("/api/missions/m-1/events?limit=1");

        var missionPayload = await missionsResponse.Content.ReadFromJsonAsync<JsonElement>();
        var eventPayload = await eventsResponse.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Single(missionPayload.EnumerateArray());
        Assert.Equal("second", missionPayload[0].GetProperty("id").GetString());
        Assert.Single(eventPayload.EnumerateArray());
        Assert.Equal("e-1", eventPayload[0].GetProperty("id").GetString());
    }

    private static HttpClient CreateClient(FakeMissionRepository? missions = null, FakeMissionEventRecorder? events = null)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Development,
        });

        builder.WebHost.UseTestServer();
        builder.Services.AddAgentForemanApi();
        builder.Services.AddSingleton<IMissionRepository>(missions ?? new FakeMissionRepository());
        builder.Services.AddSingleton<IMissionEventRecorder>(events ?? new FakeMissionEventRecorder());

        var app = builder.Build();
        app.MapAgentForemanApi();
        app.StartAsync().GetAwaiter().GetResult();
        return app.GetTestClient();
    }

    private static void SeedMission(FakeMissionRepository repository, string id, MissionStatus status, int minutesAgo)
    {
        repository.Save(new Mission(
            id,
            ExternalWorkItemId: id,
            Source: "GitHub",
            Title: $"Mission {id}",
            Status: status,
            Branch: $"agent/{id}",
            PlanPath: null,
            PullRequestUrl: null,
            RetryAfter: null,
            LastError: null,
            CreatedAt: DateTimeOffset.UtcNow.AddMinutes(-minutesAgo - 10),
            UpdatedAt: DateTimeOffset.UtcNow.AddMinutes(-minutesAgo)));
    }
}
