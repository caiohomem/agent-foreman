using System.Text.Json;
using AgentForeman.Core.Commands;
using AgentForeman.Core.Configuration;
using AgentForeman.Core.WorkItems;

namespace AgentForeman.Infrastructure.WorkItems;

public sealed class GitHubWorkItemProvider : IWorkItemProvider
{
    private const string IssueJsonFields = "number,title,body,url,labels,createdAt,updatedAt";
    private readonly AgentForemanConfig _config;
    private readonly ICommandRunner _commandRunner;

    public GitHubWorkItemProvider(AgentForemanConfig config, ICommandRunner commandRunner)
    {
        _config = config;
        _commandRunner = commandRunner;
    }

    public async Task<IReadOnlyList<WorkItem>> GetReadyItemsAsync(CancellationToken cancellationToken)
    {
        var result = await RunGhAsync(new[]
        {
            "issue", "list", "--repo", _config.WorkItems.Repo, "--label", _config.WorkItems.ReadyLabel,
            "--json", IssueJsonFields,
        }, cancellationToken);

        ThrowIfFailed(result);
        return JsonSerializer.Deserialize<List<GitHubIssueDto>>(result.StdoutText, JsonOptions)
            ?.Select(ToWorkItem)
            .ToArray() ?? Array.Empty<WorkItem>();
    }

    public async Task<WorkItem> GetWorkItemAsync(string externalId, CancellationToken cancellationToken)
    {
        var result = await RunGhAsync(new[]
        {
            "issue", "view", externalId, "--repo", _config.WorkItems.Repo, "--json", IssueJsonFields,
        }, cancellationToken);

        ThrowIfFailed(result);
        var issue = JsonSerializer.Deserialize<GitHubIssueDto>(result.StdoutText, JsonOptions)
            ?? throw new InvalidOperationException("GitHub issue response was empty.");
        return ToWorkItem(issue);
    }

    public async Task MarkAsWorkingAsync(WorkItem item, CancellationToken cancellationToken)
    {
        await EditLabelAsync(item.ExternalId, "--add-label", _config.WorkItems.WorkingLabel, cancellationToken);
        await EditLabelAsync(item.ExternalId, "--remove-label", _config.WorkItems.PausedLabel, cancellationToken);
    }

    public async Task MarkAsPausedAsync(WorkItem item, string reason, DateTimeOffset retryAfter, CancellationToken cancellationToken)
    {
        await EditLabelAsync(item.ExternalId, "--add-label", _config.WorkItems.PausedLabel, cancellationToken);
        await AddCommentAsync(item, $"Paused: {reason}{Environment.NewLine}Retry after: {retryAfter:O}", cancellationToken);
    }

    public async Task MarkAsReviewAsync(WorkItem item, string pullRequestUrl, CancellationToken cancellationToken)
    {
        await EditLabelAsync(item.ExternalId, "--add-label", _config.WorkItems.ReviewLabel, cancellationToken);
        await EditLabelAsync(item.ExternalId, "--remove-label", _config.WorkItems.WorkingLabel, cancellationToken);
        await EditLabelAsync(item.ExternalId, "--remove-label", _config.WorkItems.ReadyLabel, cancellationToken);
        await AddCommentAsync(item, $"Ready for review: {pullRequestUrl}", cancellationToken);
    }

    public async Task AddCommentAsync(WorkItem item, string comment, CancellationToken cancellationToken)
    {
        var result = await RunGhAsync(new[]
        {
            "issue", "comment", item.ExternalId, "--repo", _config.WorkItems.Repo, "--body", comment,
        }, cancellationToken);

        ThrowIfFailed(result);
    }

    public async Task<WorkItem> CreateWorkItemAsync(CreateWorkItemRequest request, CancellationToken cancellationToken)
    {
        var arguments = new List<string>
        {
            "issue", "create", "--repo", _config.WorkItems.Repo, "--title", request.Title, "--body", request.Body,
        };

        foreach (var label in request.Labels)
        {
            arguments.Add("--label");
            arguments.Add(label);
        }

        var result = await RunGhAsync(arguments, cancellationToken);
        ThrowIfFailed(result);

        return new WorkItem(
            result.StdoutText.Trim(),
            WorkItemSource.GitHub,
            request.Title,
            request.Body,
            result.StdoutText.Trim(),
            _config.WorkItems.Repo,
            request.Labels.Select(label => new WorkItemLabel(label)).ToArray(),
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);
    }

    private async Task EditLabelAsync(string externalId, string operation, string label, CancellationToken cancellationToken)
    {
        var result = await RunGhAsync(new[]
        {
            "issue", "edit", externalId, "--repo", _config.WorkItems.Repo, operation, label,
        }, cancellationToken);

        ThrowIfFailed(result);
    }

    private Task<CommandResult> RunGhAsync(IReadOnlyList<string> arguments, CancellationToken cancellationToken)
    {
        return _commandRunner.RunAsync(new CommandRequest("gh", arguments), cancellationToken: cancellationToken);
    }

    private WorkItem ToWorkItem(GitHubIssueDto issue)
    {
        return new WorkItem(
            issue.Number.ToString(),
            WorkItemSource.GitHub,
            issue.Title ?? string.Empty,
            issue.Body ?? string.Empty,
            issue.Url ?? string.Empty,
            _config.WorkItems.Repo,
            issue.Labels?.Select(label => new WorkItemLabel(label.Name ?? string.Empty)).ToArray() ?? Array.Empty<WorkItemLabel>(),
            issue.CreatedAt,
            issue.UpdatedAt);
    }

    private static void ThrowIfFailed(CommandResult result)
    {
        if (!result.Success)
        {
            throw new InvalidOperationException(result.StderrText);
        }
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private sealed class GitHubIssueDto
    {
        public int Number { get; set; }
        public string? Title { get; set; }
        public string? Body { get; set; }
        public string? Url { get; set; }
        public List<GitHubLabelDto>? Labels { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
    }

    private sealed class GitHubLabelDto
    {
        public string? Name { get; set; }
    }
}
