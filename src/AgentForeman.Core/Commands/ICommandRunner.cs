namespace AgentForeman.Core.Commands;

public interface ICommandRunner
{
    Task<CommandResult> RunAsync(
        CommandRequest request,
        Action<CommandOutputLine>? onOutputLine = null,
        CancellationToken cancellationToken = default);
}
