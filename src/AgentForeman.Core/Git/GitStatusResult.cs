namespace AgentForeman.Core.Git;

public sealed record GitStatusResult(IReadOnlyList<GitChangedFile> ChangedFiles);
