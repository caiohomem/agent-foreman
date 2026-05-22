using AgentForeman.Core.Configuration;
using AgentForeman.Core.Git;
using AgentForeman.Core.Safety;

namespace AgentForeman.Infrastructure.Safety;

public sealed class GitSafetyChecker : ISafetyChecker
{
    private readonly IGitRepository _gitRepository;

    public GitSafetyChecker(IGitRepository gitRepository)
    {
        _gitRepository = gitRepository;
    }

    public async Task<SafetyCheckResult> CheckAsync(string repoPath, SafetyConfig safetyConfig, CancellationToken cancellationToken)
    {
        var status = await _gitRepository.GetStatusAsync(repoPath, cancellationToken);
        var violations = new List<SafetyViolation>();

        if (safetyConfig.MaxFilesChanged.HasValue && status.ChangedFiles.Count > safetyConfig.MaxFilesChanged.Value)
        {
            violations.Add(new SafetyViolation(
                $"Too many changed files: {status.ChangedFiles.Count} (max {safetyConfig.MaxFilesChanged.Value})."));
        }

        foreach (var file in status.ChangedFiles)
        {
            foreach (var forbidden in safetyConfig.ForbiddenPaths)
            {
                if (string.IsNullOrWhiteSpace(forbidden))
                    continue;

                if (IsForbidden(file.Path, forbidden))
                {
                    violations.Add(new SafetyViolation($"Forbidden file changed: {file.Path}"));
                    break;
                }
            }
        }

        return violations.Count == 0 ? SafetyCheckResult.Ok() : SafetyCheckResult.Fail(violations);
    }

    private static bool IsForbidden(string filePath, string forbiddenPath)
    {
        if (forbiddenPath.EndsWith('/'))
            return filePath.StartsWith(forbiddenPath, StringComparison.OrdinalIgnoreCase);

        return string.Equals(filePath, forbiddenPath, StringComparison.OrdinalIgnoreCase);
    }
}
