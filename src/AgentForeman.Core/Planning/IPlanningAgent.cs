namespace AgentForeman.Core.Planning;

public interface IPlanningAgent
{
    Task<PlanningResult> CreatePlanAsync(PlanningRequest request, CancellationToken cancellationToken);
}

