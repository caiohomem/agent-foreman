using AgentForeman.Core.Coding;
using AgentForeman.Core.Commands;
using AgentForeman.Core.Configuration;

namespace AgentForeman.Infrastructure.Coding;

public static class CodingAgentFactory
{
    public const string CodexCliProvider = "codex-cli";
    public const string OpencodeCliProvider = "opencode-cli";

    public static readonly IReadOnlyList<string> SupportedProviders = new[]
    {
        CodexCliProvider,
        OpencodeCliProvider,
    };

    public static bool IsSupported(string provider)
    {
        return SupportedProviders.Any(supported =>
            string.Equals(supported, provider, StringComparison.OrdinalIgnoreCase));
    }

    public static ICodingAgent Create(AgentForemanConfig config, ICommandRunner commandRunner)
    {
        var provider = config.Executor.Provider ?? string.Empty;

        if (string.Equals(provider, OpencodeCliProvider, StringComparison.OrdinalIgnoreCase))
        {
            return new OpencodeCliCodingAgent(commandRunner);
        }

        if (string.Equals(provider, CodexCliProvider, StringComparison.OrdinalIgnoreCase))
        {
            return new CodexCliCodingAgent(commandRunner);
        }

        throw new InvalidOperationException(
            $"Unsupported executor provider: {provider}. Supported: {string.Join(", ", SupportedProviders)}.");
    }
}
