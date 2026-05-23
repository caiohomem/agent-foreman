namespace AgentForeman.Core.Labels;

public sealed record LabelSyncResult(IReadOnlyList<LabelSyncItemResult> Results);

public sealed record LabelSyncItemResult(string Name, bool Created, bool Existed);

