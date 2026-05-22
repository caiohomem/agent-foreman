namespace AgentForeman.Core.Testing;

public sealed record TestRunResult(
    bool Success,
    string LogPath,
    IReadOnlyList<TestCommandResult> CommandResults,
    DateTimeOffset StartedAt,
    DateTimeOffset FinishedAt,
    string? ErrorMessage);
