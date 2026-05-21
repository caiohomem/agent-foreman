namespace AgentForeman.Core.Commands;

public sealed record CommandOutputLine(
    CommandOutputStream Stream,
    string Content,
    DateTimeOffset Timestamp);
