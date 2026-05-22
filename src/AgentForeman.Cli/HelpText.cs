namespace AgentForeman.Cli;

public static class HelpText
{
    public const string Value = """
        agent-foreman

        Mission control for AI coding agents.

        Usage:
          agent-foreman <command>

        Commands:
          help      Show this help text.
          config    Validate and inspect configuration.
          state     Manage local state database.
          exec      Run an external command through Agent Foreman.
          git       Inspect and manage the configured git repository.
          work-items Inspect configured work items.
          plan      Create a technical plan for a work item.
          execute   Execute a planned work item with Codex.
          verify    Run safety checks and configured tests.
          submit    Commit verified changes, push a branch and create a pull request.
          run-once  Run one ready work item.
          daemon    Watch for ready work items.
          status    Show mission status.
          resume    Resume a paused mission.
          cancel    Cancel a mission.
          doctor    Check local prerequisites.

        """;
}
