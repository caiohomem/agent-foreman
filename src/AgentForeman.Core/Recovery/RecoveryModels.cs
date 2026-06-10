using AgentForeman.Core.Configuration;
using AgentForeman.Core.State;

namespace AgentForeman.Core.Recovery;

public enum FailedStage { BranchPrep, Planning, Coding, Safety, Tests, Submit }
public enum RecoveryCategory { DirtyWorktree, Transient, Quota, CodeError, ConfigError, NeedsHuman }

public sealed record RecoveryRequest(
    string MissionId, string ExternalWorkItemId, FailedStage FailedStage, string LastError,
    string Stdout, string Stderr, string GitStatusText, int AttemptNumber,
    IReadOnlyList<Lesson> SimilarLessons, AgentForemanConfig Config, string OutputDirectory);

public sealed record RecoveryDiagnosis(
    RecoveryCategory Category, string Diagnosis, string ProposedAction,
    string LessonTitle, string LessonBody, double Confidence);

public sealed record RemediationContext(string MissionId, string RepoPath, FailedStage FailedStage);
public sealed record RemediationResult(bool Success, bool RetryStage, bool PauseQuota, bool StartTestRepair, string? ErrorMessage);
