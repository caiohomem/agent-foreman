using AgentForeman.Cli;

namespace AgentForeman.Tests;

public sealed class CliHelpTests
{
    private const string ExpectedHelp = """
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
          labels    Manage Agent Foreman GitHub labels.
          work-items Inspect configured work items.
          plan      Create a technical plan for a work item.
          execute   Execute a planned work item with Codex.
          verify    Run safety checks and configured tests.
          submit    Commit verified changes, push a branch and create a pull request.
          run-once  Run one ready work item.
          daemon    Watch for ready work items.
          status    Show mission status.
          sync      Synchronize local mission state with GitHub issues.
          events    Show structured mission events.
          resume    Resume a paused mission.
          cancel    Cancel a mission.
          doctor    Check local prerequisites.

        """;

    [Theory]
    [MemberData(nameof(HelpArgumentSets))]
    public void HelpArgumentsPrintTheSameHelpText(string[] args)
    {
        var result = CliApplication.Execute(args);

        Assert.Equal(0, result.ExitCode);
        Assert.Equal(ExpectedHelp, result.Output);
        Assert.Equal(string.Empty, result.Error);
    }

    public static TheoryData<string[]> HelpArgumentSets()
    {
        return new TheoryData<string[]>
        {
            Array.Empty<string>(),
            new[] { "help" },
            new[] { "--help" },
            new[] { "-h" },
        };
    }

    [Fact]
    public void UnknownCommandReturnsNonZeroExitCode()
    {
        var result = CliApplication.Execute(new[] { "banana" });

        Assert.NotEqual(0, result.ExitCode);
    }

    [Fact]
    public void UnknownCommandPrintsCommandNameAndHelpHint()
    {
        var result = CliApplication.Execute(new[] { "banana" });

        Assert.Equal(string.Empty, result.Output);
        Assert.Equal(
            """
            Unknown command: banana

            Run `agent-foreman help` to see available commands.

            """,
            result.Error);
    }
}
