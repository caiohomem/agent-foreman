namespace AgentForeman.Cli;

public sealed record CommandResult(int ExitCode, string Output, string Error);
