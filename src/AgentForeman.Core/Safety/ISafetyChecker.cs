using AgentForeman.Core.Configuration;

namespace AgentForeman.Core.Safety;

public interface ISafetyChecker
{
    Task<SafetyCheckResult> CheckAsync(string repoPath, SafetyConfig safetyConfig, CancellationToken cancellationToken);
}
