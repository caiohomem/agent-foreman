using AgentForeman.Core.Events;
using Npgsql;

namespace AgentForeman.Infrastructure.State;

public sealed class PostgresMissionEventRecorder : IMissionEventRecorder
{
    private readonly string _connectionString;

    public PostgresMissionEventRecorder(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task AppendMissionEventAsync(MissionEvent missionEvent, CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            INSERT INTO mission_events (
                id, mission_id, external_work_item_id, run_id, event_type, level, message, metadata_json, created_at
            )
            VALUES (
                @id, @mission_id, @external_work_item_id, @run_id, @event_type, @level, @message, @metadata_json::jsonb, @created_at
            );
            """,
            connection);

        command.Parameters.AddWithValue("id", missionEvent.Id);
        command.Parameters.AddWithValue("mission_id", missionEvent.MissionId);
        command.Parameters.AddWithValue("external_work_item_id", (object?)missionEvent.ExternalWorkItemId ?? DBNull.Value);
        command.Parameters.AddWithValue("run_id", (object?)missionEvent.RunId ?? DBNull.Value);
        command.Parameters.AddWithValue("event_type", missionEvent.EventType.ToString());
        command.Parameters.AddWithValue("level", missionEvent.Level.ToString());
        command.Parameters.AddWithValue("message", missionEvent.Message);
        command.Parameters.AddWithValue("metadata_json", (object?)missionEvent.MetadataJson ?? DBNull.Value);
        command.Parameters.AddWithValue("created_at", missionEvent.CreatedAt);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MissionEvent>> GetMissionEventsAsync(string missionId, int limit, CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            SELECT id, mission_id, external_work_item_id, run_id, event_type, level, message, metadata_json, created_at
            FROM (
                SELECT id, mission_id, external_work_item_id, run_id, event_type, level, message, metadata_json, created_at
                FROM mission_events
                WHERE mission_id = @mission_id
                ORDER BY created_at DESC
                LIMIT @limit
            ) events
            ORDER BY created_at ASC;
            """,
            connection);

        command.Parameters.AddWithValue("mission_id", missionId);
        command.Parameters.AddWithValue("limit", limit);
        return await ReadEventsAsync(command, cancellationToken);
    }

    public async Task<IReadOnlyList<MissionEvent>> GetRecentMissionEventsAsync(int limit, CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            SELECT id, mission_id, external_work_item_id, run_id, event_type, level, message, metadata_json, created_at
            FROM (
                SELECT id, mission_id, external_work_item_id, run_id, event_type, level, message, metadata_json, created_at
                FROM mission_events
                ORDER BY created_at DESC
                LIMIT @limit
            ) events
            ORDER BY created_at ASC;
            """,
            connection);

        command.Parameters.AddWithValue("limit", limit);
        return await ReadEventsAsync(command, cancellationToken);
    }

    private static async Task<IReadOnlyList<MissionEvent>> ReadEventsAsync(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var events = new List<MissionEvent>();

        while (await reader.ReadAsync(cancellationToken))
        {
            events.Add(new MissionEvent(
                reader.GetString(0),
                reader.GetString(1),
                reader.IsDBNull(2) ? null : reader.GetString(2),
                reader.IsDBNull(3) ? null : reader.GetString(3),
                Enum.Parse<MissionEventType>(reader.GetString(4)),
                Enum.Parse<MissionEventLevel>(reader.GetString(5)),
                reader.GetString(6),
                reader.IsDBNull(7) ? null : reader.GetString(7),
                reader.GetFieldValue<DateTimeOffset>(8)));
        }

        return events;
    }

    private async Task<NpgsqlConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}
