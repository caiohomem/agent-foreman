using System.Text.Json;
using AgentForeman.Core.Commands;
using AgentForeman.Core.Configuration;
using AgentForeman.Core.Labels;

namespace AgentForeman.Infrastructure.Labels;

public sealed class GitHubLabelManager : ILabelManager
{
    private readonly ICommandRunner _commandRunner;

    public GitHubLabelManager(ICommandRunner commandRunner)
    {
        _commandRunner = commandRunner;
    }

    public async Task<IReadOnlyList<string>> ListAsync(string repository, CancellationToken cancellationToken)
    {
        var result = await _commandRunner.RunAsync(
            new CommandRequest("gh", new[] { "label", "list", "--repo", repository, "--json", "name" }),
            cancellationToken: cancellationToken);

        if (!result.Success)
        {
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(result.StderrText) ? "gh label list failed." : result.StderrText.Trim());
        }

        return JsonSerializer.Deserialize<List<GitHubLabelDto>>(result.StdoutText, JsonOptions)
            ?.Select(label => label.Name ?? string.Empty)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToArray() ?? Array.Empty<string>();
    }

    public async Task<LabelSyncResult> SyncAsync(AgentForemanConfig config, CancellationToken cancellationToken)
    {
        var existing = new HashSet<string>(await ListAsync(config.WorkItems.Repo, cancellationToken), StringComparer.OrdinalIgnoreCase);
        var results = new List<LabelSyncItemResult>();

        foreach (var label in GetRequiredLabels(config))
        {
            if (existing.Contains(label.Name))
            {
                results.Add(new LabelSyncItemResult(label.Name, Created: false, Existed: true));
                continue;
            }

            var result = await _commandRunner.RunAsync(
                new CommandRequest(
                    "gh",
                    new[]
                    {
                        "label", "create", label.Name,
                        "--repo", config.WorkItems.Repo,
                        "--color", label.Color,
                        "--description", label.Description,
                    }),
                cancellationToken: cancellationToken);

            if (!result.Success)
            {
                if (result.StderrText.Contains("already exists", StringComparison.OrdinalIgnoreCase))
                {
                    results.Add(new LabelSyncItemResult(label.Name, Created: false, Existed: true));
                    continue;
                }

                throw new InvalidOperationException(string.IsNullOrWhiteSpace(result.StderrText) ? $"gh label create failed for {label.Name}." : result.StderrText.Trim());
            }

            results.Add(new LabelSyncItemResult(label.Name, Created: true, Existed: false));
        }

        return new LabelSyncResult(results);
    }

    internal static IReadOnlyList<LabelDefinition> GetRequiredLabels(AgentForemanConfig config)
    {
        return new[]
        {
            new LabelDefinition(config.WorkItems.ReadyLabel, "0e8a16", "Ready for Agent Foreman to process."),
            new LabelDefinition(config.WorkItems.WorkingLabel, "1d76db", "Currently being processed by Agent Foreman."),
            new LabelDefinition(config.WorkItems.ReviewLabel, "fbca04", "Pull request created by Agent Foreman and awaiting human review."),
            new LabelDefinition(config.WorkItems.PausedLabel, "d93f0b", "Paused by Agent Foreman, usually due to quota or retry window."),
            new LabelDefinition(string.IsNullOrWhiteSpace(config.WorkItems.BlockedLabel) ? "agent-blocked" : config.WorkItems.BlockedLabel, "b60205", "Blocked because dependencies are not completed."),
            new LabelDefinition(string.IsNullOrWhiteSpace(config.WorkItems.FailedLabel) ? "agent-failed" : config.WorkItems.FailedLabel, "5319e7", "Agent Foreman failed while processing this work item."),
        };
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private sealed class GitHubLabelDto
    {
        public string? Name { get; set; }
    }
}
