namespace AgentForeman.Core.Safety;

public sealed record SafetyCheckResult(bool Passed, IReadOnlyList<SafetyViolation> Violations)
{
    public static SafetyCheckResult Ok() =>
        new(true, Array.Empty<SafetyViolation>());

    public static SafetyCheckResult Fail(IReadOnlyList<SafetyViolation> violations) =>
        new(false, violations);
}
