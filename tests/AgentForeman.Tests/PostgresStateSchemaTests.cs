using AgentForeman.Infrastructure.State;

namespace AgentForeman.Tests;

public sealed class PostgresStateSchemaTests
{
    [Theory]
    [InlineData("missions")]
    [InlineData("mission_runs")]
    [InlineData("mission_logs")]
    [InlineData("provider_states")]
    public void SchemaCreationScriptIncludesRequiredTables(string tableName)
    {
        Assert.Contains($"CREATE TABLE IF NOT EXISTS {tableName}", PostgresStateSchema.CreateSchemaSql);
    }
}
