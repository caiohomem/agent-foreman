using AgentForeman.Core.State;
using AgentForeman.Core.Summaries;
using Npgsql;

namespace AgentForeman.Infrastructure.State;

public sealed class PostgresRunSummaryRepository : IRunSummaryRepository
{
    private readonly string _connectionString;

    public PostgresRunSummaryRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task SaveRunSummaryAsync(RunSummary runSummary, CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            INSERT INTO agent_run_summaries (
                id, mission_id, external_work_item_id, summary_type, content, path, created_at
            )
            VALUES (
                @id, @mission_id, @external_work_item_id, @summary_type, @content, @path, @created_at
            );
            """,
            connection);

        command.Parameters.AddWithValue("id", runSummary.Id);
        command.Parameters.AddWithValue("mission_id", runSummary.MissionId);
        command.Parameters.AddWithValue("external_work_item_id", (object?)runSummary.ExternalWorkItemId ?? DBNull.Value);
        command.Parameters.AddWithValue("summary_type", runSummary.SummaryType.ToString());
        command.Parameters.AddWithValue("content", runSummary.Content);
        command.Parameters.AddWithValue("path", (object?)runSummary.Path ?? DBNull.Value);
        command.Parameters.AddWithValue("created_at", runSummary.CreatedAt);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RunSummary>> GetRunSummariesAsync(string missionId, CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            SELECT id, mission_id, external_work_item_id, summary_type, content, path, created_at
            FROM agent_run_summaries
            WHERE mission_id = @mission_id
            ORDER BY created_at ASC;
            """,
            connection);
        command.Parameters.AddWithValue("mission_id", missionId);
        return await ReadSummariesAsync(command, cancellationToken);
    }

    public async Task<RunSummary?> GetLatestRunSummaryAsync(string missionId, RunSummaryType? summaryType, CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            summaryType is null
                ? """
                  SELECT id, mission_id, external_work_item_id, summary_type, content, path, created_at
                  FROM agent_run_summaries
                  WHERE mission_id = @mission_id
                  ORDER BY created_at DESC
                  LIMIT 1;
                  """
                : """
                  SELECT id, mission_id, external_work_item_id, summary_type, content, path, created_at
                  FROM agent_run_summaries
                  WHERE mission_id = @mission_id
                    AND summary_type = @summary_type
                  ORDER BY created_at DESC
                  LIMIT 1;
                  """,
            connection);

        command.Parameters.AddWithValue("mission_id", missionId);
        if (summaryType is not null)
        {
            command.Parameters.AddWithValue("summary_type", summaryType.Value.ToString());
        }

        var summaries = await ReadSummariesAsync(command, cancellationToken);
        return summaries.FirstOrDefault();
    }

    private static async Task<IReadOnlyList<RunSummary>> ReadSummariesAsync(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var summaries = new List<RunSummary>();

        while (await reader.ReadAsync(cancellationToken))
        {
            summaries.Add(new RunSummary(
                reader.GetString(0),
                reader.GetString(1),
                reader.IsDBNull(2) ? null : reader.GetString(2),
                Enum.Parse<RunSummaryType>(reader.GetString(3)),
                reader.GetString(4),
                reader.IsDBNull(5) ? null : reader.GetString(5),
                reader.GetFieldValue<DateTimeOffset>(6)));
        }

        return summaries;
    }

    private async Task<NpgsqlConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}
