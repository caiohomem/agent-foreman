using AgentForeman.Infrastructure.State;

namespace AgentForeman.Tests;

public sealed class PostgresStateSchemaTests
{
    [Theory]
    [InlineData("missions")]
    [InlineData("mission_runs")]
    [InlineData("mission_logs")]
    [InlineData("mission_events")]
    [InlineData("provider_states")]
    public void SchemaCreationScriptIncludesRequiredTables(string tableName)
    {
        Assert.Contains($"CREATE TABLE IF NOT EXISTS {tableName}", PostgresStateSchema.CreateSchemaSql);
    }

    [Fact]
    public void SchemaCreationScriptIncludesBlockedCommentColumnUpdate()
    {
        Assert.Contains("ALTER TABLE missions ADD COLUMN IF NOT EXISTS blocked_comment_posted_at TIMESTAMPTZ NULL;", PostgresStateSchema.CreateSchemaSql);
    }
}
