namespace AgentForeman.Core.Prerequisites;

public interface ICommandAvailabilityChecker
{
    bool IsAvailable(string command);
}
