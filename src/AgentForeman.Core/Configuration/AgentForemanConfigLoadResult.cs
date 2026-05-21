namespace AgentForeman.Core.Configuration;

public sealed class AgentForemanConfigLoadResult
{
    private AgentForemanConfigLoadResult(AgentForemanConfig? config, IReadOnlyList<string> errors)
    {
        Config = config;
        Errors = errors;
    }

    public AgentForemanConfig? Config { get; }
    public IReadOnlyList<string> Errors { get; }
    public bool IsValid => Errors.Count == 0;

    public static AgentForemanConfigLoadResult Success(AgentForemanConfig config)
    {
        return new AgentForemanConfigLoadResult(config, Array.Empty<string>());
    }

    public static AgentForemanConfigLoadResult Failure(IReadOnlyList<string> errors)
    {
        return new AgentForemanConfigLoadResult(null, errors);
    }
}
