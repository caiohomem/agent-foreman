namespace AgentForeman.Core.Commands;

public sealed record CommandRequest(
    string Executable,
    IReadOnlyList<string> Arguments,
    string? WorkingDirectory = null,
    IReadOnlyDictionary<string, string>? EnvironmentVariables = null,
    TimeSpan? Timeout = null,
    bool CaptureStdout = true,
    bool CaptureStderr = true);
