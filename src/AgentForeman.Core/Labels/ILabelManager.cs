using AgentForeman.Core.Configuration;

namespace AgentForeman.Core.Labels;

public interface ILabelManager
{
    Task<IReadOnlyList<string>> ListAsync(string repository, CancellationToken cancellationToken);
    Task<LabelSyncResult> SyncAsync(AgentForemanConfig config, CancellationToken cancellationToken);
}

