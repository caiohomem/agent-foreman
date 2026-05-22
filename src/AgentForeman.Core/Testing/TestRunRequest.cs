namespace AgentForeman.Core.Testing;

public sealed record TestRunRequest(
    string WorkItemId,
    string RepoPath,
    IReadOnlyList<string> Commands,
    string OutputDirectory);
