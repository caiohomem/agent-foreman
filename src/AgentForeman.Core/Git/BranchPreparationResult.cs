namespace AgentForeman.Core.Git;

public sealed record BranchPreparationResult(bool Success, string? ErrorMessage)
{
    public static BranchPreparationResult Ok() => new(true, null);
    public static BranchPreparationResult Fail(string message) => new(false, message);
}
