using AgentForeman.Core.State;
using Npgsql;

namespace AgentForeman.Infrastructure.State;

public sealed class PostgresMissionRepository : IMissionRepository
{
    private readonly string _connectionString;

    public PostgresMissionRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public Mission? GetById(string id)
    {
        using var connection = OpenConnection();
        using var command = new NpgsqlCommand(
            """
            SELECT id, external_work_item_id, source, title, status, branch, plan_path, pull_request_url,
                   retry_after, last_error, created_at, updated_at
            FROM missions
            WHERE id = @id;
            """,
            connection);
        command.Parameters.AddWithValue("id", id);

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        return new Mission(
            reader.GetString(0),
            reader.IsDBNull(1) ? null : reader.GetString(1),
            reader.IsDBNull(2) ? null : reader.GetString(2),
            reader.GetString(3),
            Enum.Parse<MissionStatus>(reader.GetString(4)),
            reader.IsDBNull(5) ? null : reader.GetString(5),
            reader.IsDBNull(6) ? null : reader.GetString(6),
            reader.IsDBNull(7) ? null : reader.GetString(7),
            reader.IsDBNull(8) ? null : reader.GetFieldValue<DateTimeOffset>(8),
            reader.IsDBNull(9) ? null : reader.GetString(9),
            reader.GetFieldValue<DateTimeOffset>(10),
            reader.GetFieldValue<DateTimeOffset>(11));
    }

    public void Save(Mission mission)
    {
        using var connection = OpenConnection();
        using var command = new NpgsqlCommand(
            """
            INSERT INTO missions (
                id, external_work_item_id, source, title, status, branch, plan_path, pull_request_url,
                retry_after, last_error, created_at, updated_at
            )
            VALUES (
                @id, @external_work_item_id, @source, @title, @status, @branch, @plan_path, @pull_request_url,
                @retry_after, @last_error, @created_at, @updated_at
            )
            ON CONFLICT (id) DO UPDATE SET
                external_work_item_id = EXCLUDED.external_work_item_id,
                source = EXCLUDED.source,
                title = EXCLUDED.title,
                status = EXCLUDED.status,
                branch = EXCLUDED.branch,
                plan_path = EXCLUDED.plan_path,
                pull_request_url = EXCLUDED.pull_request_url,
                retry_after = EXCLUDED.retry_after,
                last_error = EXCLUDED.last_error,
                updated_at = EXCLUDED.updated_at;
            """,
            connection);

        command.Parameters.AddWithValue("id", mission.Id);
        command.Parameters.AddWithValue("external_work_item_id", (object?)mission.ExternalWorkItemId ?? DBNull.Value);
        command.Parameters.AddWithValue("source", (object?)mission.Source ?? DBNull.Value);
        command.Parameters.AddWithValue("title", mission.Title);
        command.Parameters.AddWithValue("status", mission.Status.ToString());
        command.Parameters.AddWithValue("branch", (object?)mission.Branch ?? DBNull.Value);
        command.Parameters.AddWithValue("plan_path", (object?)mission.PlanPath ?? DBNull.Value);
        command.Parameters.AddWithValue("pull_request_url", (object?)mission.PullRequestUrl ?? DBNull.Value);
        command.Parameters.AddWithValue("retry_after", (object?)mission.RetryAfter ?? DBNull.Value);
        command.Parameters.AddWithValue("last_error", (object?)mission.LastError ?? DBNull.Value);
        command.Parameters.AddWithValue("created_at", mission.CreatedAt);
        command.Parameters.AddWithValue("updated_at", mission.UpdatedAt);
        command.ExecuteNonQuery();
    }

    private NpgsqlConnection OpenConnection()
    {
        var connection = new NpgsqlConnection(_connectionString);
        connection.Open();
        return connection;
    }
}
