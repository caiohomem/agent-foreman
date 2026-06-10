using AgentForeman.Core.State;
using Npgsql;

namespace AgentForeman.Infrastructure.State;

public sealed class PostgresLessonRepository : ILessonRepository
{
    private readonly string _connectionString;

    public PostgresLessonRepository(string connectionString) => _connectionString = connectionString;

    public async Task SaveAsync(Lesson lesson, CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            INSERT INTO agent_lessons
                (id, mission_id, external_work_item_id, category, title, body, outcome, source, created_at)
            VALUES
                (@id, @mission_id, @external_work_item_id, @category, @title, @body, @outcome, @source, @created_at)
            ON CONFLICT (id) DO UPDATE SET outcome = EXCLUDED.outcome, body = EXCLUDED.body;
            """, connection);
        command.Parameters.AddWithValue("id", lesson.Id);
        command.Parameters.AddWithValue("mission_id", (object?)lesson.MissionId ?? DBNull.Value);
        command.Parameters.AddWithValue("external_work_item_id", (object?)lesson.ExternalWorkItemId ?? DBNull.Value);
        command.Parameters.AddWithValue("category", lesson.Category);
        command.Parameters.AddWithValue("title", lesson.Title);
        command.Parameters.AddWithValue("body", lesson.Body);
        command.Parameters.AddWithValue("outcome", lesson.Outcome);
        command.Parameters.AddWithValue("source", lesson.Source);
        command.Parameters.AddWithValue("created_at", lesson.CreatedAt);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Lesson>> SearchAsync(string query, int topK, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query))
            return await GetRecentAsync(topK, cancellationToken);

        await using var connection = await OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            SELECT id, mission_id, external_work_item_id, category, title, body, outcome, source, created_at
            FROM agent_lessons
            WHERE search_tsv @@ websearch_to_tsquery('simple', @query)
            ORDER BY ts_rank(search_tsv, websearch_to_tsquery('simple', @query)) DESC, created_at DESC
            LIMIT @limit;
            """, connection);
        command.Parameters.AddWithValue("query", query);
        command.Parameters.AddWithValue("limit", topK);
        var results = await ReadAsync(command, cancellationToken);
        return results.Count > 0 ? results : await GetRecentAsync(topK, cancellationToken);
    }

    public async Task<IReadOnlyList<Lesson>> GetRecentAsync(int topK, CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            SELECT id, mission_id, external_work_item_id, category, title, body, outcome, source, created_at
            FROM agent_lessons ORDER BY created_at DESC LIMIT @limit;
            """, connection);
        command.Parameters.AddWithValue("limit", topK);
        return await ReadAsync(command, cancellationToken);
    }

    private static async Task<IReadOnlyList<Lesson>> ReadAsync(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var lessons = new List<Lesson>();
        while (await reader.ReadAsync(cancellationToken))
            lessons.Add(new Lesson(reader.GetString(0), reader.IsDBNull(1) ? null : reader.GetString(1),
                reader.IsDBNull(2) ? null : reader.GetString(2), reader.GetString(3), reader.GetString(4),
                reader.GetString(5), reader.GetString(6), reader.GetString(7), reader.GetFieldValue<DateTimeOffset>(8)));
        return lessons;
    }

    private async Task<NpgsqlConnection> OpenAsync(CancellationToken cancellationToken)
    {
        var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}
