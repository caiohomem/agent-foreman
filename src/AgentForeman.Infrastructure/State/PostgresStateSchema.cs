namespace AgentForeman.Infrastructure.State;

public static class PostgresStateSchema
{
    public const string CreateSchemaSql = """
        CREATE TABLE IF NOT EXISTS missions (
            id TEXT PRIMARY KEY,
            external_work_item_id TEXT NULL,
            source TEXT NULL,
            title TEXT NOT NULL,
            status TEXT NOT NULL,
            branch TEXT NULL,
            plan_path TEXT NULL,
            pull_request_url TEXT NULL,
            retry_after TIMESTAMPTZ NULL,
            last_error TEXT NULL,
            created_at TIMESTAMPTZ NOT NULL,
            updated_at TIMESTAMPTZ NOT NULL
        );

        CREATE TABLE IF NOT EXISTS mission_runs (
            id TEXT PRIMARY KEY,
            mission_id TEXT NOT NULL,
            step TEXT NOT NULL,
            status TEXT NOT NULL,
            log_path TEXT NULL,
            started_at TIMESTAMPTZ NOT NULL,
            finished_at TIMESTAMPTZ NULL,
            exit_code INTEGER NULL,
            FOREIGN KEY(mission_id) REFERENCES missions(id)
        );

        CREATE TABLE IF NOT EXISTS mission_logs (
            id BIGSERIAL PRIMARY KEY,
            mission_id TEXT NOT NULL,
            run_id TEXT NULL,
            sequence INTEGER NOT NULL,
            stream TEXT NOT NULL,
            content TEXT NOT NULL,
            created_at TIMESTAMPTZ NOT NULL
        );

        CREATE TABLE IF NOT EXISTS provider_states (
            provider TEXT PRIMARY KEY,
            status TEXT NOT NULL,
            last_success_at TIMESTAMPTZ NULL,
            last_limit_at TIMESTAMPTZ NULL,
            retry_after TIMESTAMPTZ NULL,
            consecutive_failures INTEGER NOT NULL DEFAULT 0,
            updated_at TIMESTAMPTZ NOT NULL
        );
        """;
}
