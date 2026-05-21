using AgentForeman.Core.Configuration;
using AgentForeman.Core.State;
using Npgsql;

namespace AgentForeman.Infrastructure.State;

public sealed class PostgresStateStore : IStateStore
{
    public void Initialize(AgentForemanConfig config)
    {
        using var connection = OpenConnection(config);
        using var command = new NpgsqlCommand(PostgresStateSchema.CreateSchemaSql, connection);
        command.ExecuteNonQuery();
    }

    public StateStoreStatus GetStatus(AgentForemanConfig config)
    {
        using var connection = OpenConnection(config);
        var missionCount = CountRows(connection, "missions");
        var providerStateCount = CountRows(connection, "provider_states");

        return new StateStoreStatus(config.Database.Provider, missionCount, providerStateCount);
    }

    private static NpgsqlConnection OpenConnection(AgentForemanConfig config)
    {
        var connection = new NpgsqlConnection(config.Database.ConnectionString);
        connection.Open();
        return connection;
    }

    private static int CountRows(NpgsqlConnection connection, string tableName)
    {
        using var command = new NpgsqlCommand($"SELECT COUNT(*) FROM {tableName};", connection);
        return Convert.ToInt32(command.ExecuteScalar());
    }
}
