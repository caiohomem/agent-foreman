namespace AgentForeman.Core.Testing;

public sealed record TestCommandResult(
    string Command,
    int ExitCode,
    string Stdout,
    string Stderr,
    TimeSpan Duration)
{
    public bool Success => ExitCode == 0;
}
