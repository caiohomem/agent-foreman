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
          run-once  Run one ready work item. (future)
          daemon    Watch for ready work items. (future)
          status    Show mission status. (future)
          resume    Resume a paused mission. (future)
          cancel    Cancel a mission. (future)
          doctor    Check local prerequisites.

        """;
}
