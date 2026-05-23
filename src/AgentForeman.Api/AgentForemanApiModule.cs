using AgentForeman.Core.Events;
using AgentForeman.Core.State;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace AgentForeman.Api;

public static class AgentForemanApiModule
{
    public static IServiceCollection AddAgentForemanApi(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddOpenApi();
        services.AddSwaggerGen();
        services.AddSingleton<MissionDashboardQueries>();
        return services;
    }

    public static IEndpointRouteBuilder MapAgentForemanApi(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api");

        group.MapGet("/health", () => TypedResults.Ok(new HealthResponseDto("ok")))
            .WithName("GetHealth");

        group.MapGet("/dashboard/summary", (MissionDashboardQueries queries) =>
                TypedResults.Ok(queries.GetSummary()))
            .WithName("GetDashboardSummary");

        group.MapGet("/missions", Results<Ok<IReadOnlyList<MissionResponseDto>>, BadRequest<ProblemDetails>> (
            [FromQuery] string? status,
            [FromQuery] int? limit,
            MissionDashboardQueries queries) =>
        {
            if (!TryParseStatus(status, out var parsedStatus))
            {
                return TypedResults.BadRequest(new ProblemDetails
                {
                    Title = "Invalid mission status.",
                    Detail = $"Status '{status}' is not recognized.",
                    Status = StatusCodes.Status400BadRequest,
                });
            }

            return TypedResults.Ok(queries.GetMissions(parsedStatus, limit));
        }).WithName("GetMissions");

        group.MapGet("/missions/{id}", Results<Ok<MissionResponseDto>, NotFound> (
            string id,
            MissionDashboardQueries queries) =>
        {
            var mission = queries.GetMission(id);
            return mission is null ? TypedResults.NotFound() : TypedResults.Ok(mission);
        }).WithName("GetMission");

        group.MapGet("/missions/{id}/events", async Task<Ok<IReadOnlyList<MissionEventResponseDto>>> (
            string id,
            [FromQuery] int? limit,
            MissionDashboardQueries queries,
            CancellationToken cancellationToken) =>
        {
            var events = await queries.GetMissionEventsAsync(id, limit, cancellationToken);
            return TypedResults.Ok(events);
        }).WithName("GetMissionEvents");

        return endpoints;
    }

    private static bool TryParseStatus(string? rawStatus, out MissionStatus? status)
    {
        if (string.IsNullOrWhiteSpace(rawStatus))
        {
            status = null;
            return true;
        }

        if (Enum.TryParse<MissionStatus>(rawStatus, ignoreCase: true, out var parsed))
        {
            status = parsed;
            return true;
        }

        status = null;
        return false;
    }
}

public sealed class MissionDashboardQueries
{
    private static readonly MissionStatus[] ActiveMissionStatuses =
    [
        MissionStatus.New,
        MissionStatus.BranchCreated,
        MissionStatus.Planning,
        MissionStatus.PlanReady,
        MissionStatus.Coding,
        MissionStatus.CodingCompleted,
        MissionStatus.Testing,
        MissionStatus.TestsPassed,
    ];

    private static readonly MissionStatus[] FailedMissionStatuses =
    [
        MissionStatus.Failed,
        MissionStatus.TestsFailed,
    ];

    private readonly IMissionRepository _missionRepository;
    private readonly IMissionEventRecorder _missionEventRecorder;

    public MissionDashboardQueries(IMissionRepository missionRepository, IMissionEventRecorder missionEventRecorder)
    {
        _missionRepository = missionRepository;
        _missionEventRecorder = missionEventRecorder;
    }

    public DashboardSummaryResponseDto GetSummary()
    {
        var counts = Enum.GetValues<MissionStatus>()
            .ToDictionary(status => status, status => _missionRepository.GetByStatus(status, int.MaxValue).Count);

        return new DashboardSummaryResponseDto(
            TotalMissions: counts.Values.Sum(),
            ActiveMissions: Sum(counts, ActiveMissionStatuses),
            PausedMissions: counts.GetValueOrDefault(MissionStatus.PausedQuota),
            FailedMissions: Sum(counts, FailedMissionStatuses),
            ReviewMissions: counts.GetValueOrDefault(MissionStatus.PullRequestCreated),
            CompletedMissions: counts.GetValueOrDefault(MissionStatus.Completed));
    }

    public IReadOnlyList<MissionResponseDto> GetMissions(MissionStatus? status, int? limit)
    {
        var effectiveLimit = NormalizeLimit(limit, 20);
        var missions = status is null
            ? _missionRepository.GetRecent(effectiveLimit)
            : _missionRepository.GetByStatus(status.Value, effectiveLimit);

        return missions.Select(MapMission).ToList();
    }

    public MissionResponseDto? GetMission(string id)
    {
        var mission = _missionRepository.GetById(id);
        return mission is null ? null : MapMission(mission);
    }

    public async Task<IReadOnlyList<MissionEventResponseDto>> GetMissionEventsAsync(string missionId, int? limit, CancellationToken cancellationToken)
    {
        var events = await _missionEventRecorder.GetMissionEventsAsync(missionId, NormalizeLimit(limit, 50), cancellationToken);
        return events.Select(MapMissionEvent).ToList();
    }

    private static int NormalizeLimit(int? limit, int defaultValue)
    {
        if (limit is null or <= 0)
        {
            return defaultValue;
        }

        return Math.Min(limit.Value, 200);
    }

    private static int Sum(IReadOnlyDictionary<MissionStatus, int> counts, IEnumerable<MissionStatus> statuses)
    {
        var total = 0;

        foreach (var status in statuses)
        {
            total += counts.GetValueOrDefault(status);
        }

        return total;
    }

    private static MissionResponseDto MapMission(Mission mission)
    {
        return new MissionResponseDto(
            mission.Id,
            mission.ExternalWorkItemId,
            mission.Source,
            mission.Title,
            mission.Status.ToString(),
            mission.Branch,
            mission.PlanPath,
            mission.PullRequestUrl,
            mission.RetryAfter,
            mission.LastError,
            mission.CreatedAt,
            mission.UpdatedAt,
            mission.BlockedCommentPostedAt);
    }

    private static MissionEventResponseDto MapMissionEvent(MissionEvent missionEvent)
    {
        return new MissionEventResponseDto(
            missionEvent.Id,
            missionEvent.MissionId,
            missionEvent.ExternalWorkItemId,
            missionEvent.RunId,
            missionEvent.EventType.ToString(),
            missionEvent.Level.ToString(),
            missionEvent.Message,
            missionEvent.MetadataJson,
            missionEvent.CreatedAt);
    }
}
