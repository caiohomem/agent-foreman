namespace AgentForeman.Core.Configuration;

public sealed class AgentForemanConfig
{
    public ProjectConfig Project { get; init; } = new();
    public WorkItemsConfig WorkItems { get; init; } = new();
    public PlannerConfig Planner { get; init; } = new();
    public ExecutorConfig Executor { get; init; } = new();
    public TestsConfig Tests { get; init; } = new();
    public SafetyConfig Safety { get; init; } = new();
    public QuotaConfig Quota { get; init; } = new();
    public DatabaseConfig Database { get; init; } = new();
    public DaemonConfig Daemon { get; init; } = new();
    public RecoveryConfig Recovery { get; init; } = new();
    public MemoryConfig Memory { get; init; } = new();
}

public sealed class ProjectConfig
{
    public string Name { get; init; } = string.Empty;
    public string RepoPath { get; init; } = string.Empty;
    public string DefaultBranch { get; init; } = string.Empty;
}

public sealed class WorkItemsConfig
{
    public string Provider { get; init; } = string.Empty;
    public string Repo { get; init; } = string.Empty;
    public string ReadyLabel { get; init; } = string.Empty;
    public string WorkingLabel { get; init; } = string.Empty;
    public string ReviewLabel { get; init; } = string.Empty;
    public string PausedLabel { get; init; } = string.Empty;
    public string BlockedLabel { get; init; } = "agent-blocked";
    public string FailedLabel { get; init; } = "agent-failed";
}

public sealed class PlannerConfig
{
    public string Provider { get; init; } = string.Empty;
    public string Command { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty;
}

public sealed class ExecutorConfig
{
    public string Provider { get; init; } = string.Empty;
    public string Command { get; init; } = string.Empty;
    public string Sandbox { get; init; } = string.Empty;
    public string Approval { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty;
}

public sealed class TestsConfig
{
    public IReadOnlyList<string> Commands { get; init; } = Array.Empty<string>();
}

public sealed class SafetyConfig
{
    public int? MaxFilesChanged { get; init; }
    public IReadOnlyList<string> ForbiddenPaths { get; init; } = Array.Empty<string>();
    public bool AutoMergeAfterChecks { get; init; } = false;
    public string AutoMergeMethod { get; init; } = "squash";
}

public sealed class QuotaConfig
{
    public int? RetryAfterHours { get; init; }
    public IReadOnlyList<string> QuotaPatterns { get; init; } = Array.Empty<string>();
}

public sealed class DatabaseConfig
{
    public string Provider { get; init; } = string.Empty;
    public string ConnectionString { get; init; } = string.Empty;
}

public sealed class DaemonConfig
{
    public bool Enabled { get; init; } = true;
    public int PollIntervalSeconds { get; init; } = 300;
    public bool RunOnStartup { get; init; } = true;
    public bool BlockOnAnyFailedMission { get; init; } = false;
}

public sealed class RecoveryConfig
{
    public bool Enabled { get; init; } = false;
    public int MaxAttempts { get; init; } = 2;
    public int TestRepairAttempts { get; init; } = 2;
    public string Model { get; init; } = string.Empty;
}

public sealed class MemoryConfig
{
    public bool Enabled { get; init; } = false;
    public int TopKLessons { get; init; } = 3;
    public bool InjectResumeContext { get; init; } = true;
}
