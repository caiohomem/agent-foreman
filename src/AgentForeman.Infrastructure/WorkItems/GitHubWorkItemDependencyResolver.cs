using System.Text.Json;
using AgentForeman.Core.Commands;
using AgentForeman.Core.Configuration;
using AgentForeman.Core.WorkItems;

namespace AgentForeman.Infrastructure.WorkItems;

public sealed class GitHubWorkItemDependencyResolver : IWorkItemDependencyResolver
{
    private readonly AgentForemanConfig _config;
    private readonly ICommandRunner _commandRunner;

    public GitHubWorkItemDependencyResolver(AgentForemanConfig config, ICommandRunner commandRunner)
    {
        _config = config;
        _commandRunner = commandRunner;
    }

    public async Task<WorkItemDependency> ResolveAsync(WorkItemDependency dependency, CancellationToken cancellationToken)
    {
        var result = await _commandRunner.RunAsync(
            new CommandRequest(
                "gh",
                new[]
                {
                    "issue", "view", dependency.Reference,
                    "--repo", string.IsNullOrWhiteSpace(dependency.Repository) ? _config.WorkItems.Repo : dependency.Repository,
                    "--json", "number,state,title,url",
                }),
            cancellationToken: cancellationToken);

        if (!result.Success)
        {
            return dependency with { Status = WorkItemDependencyStatus.Unknown };
        }

        var issue = JsonSerializer.Deserialize<GitHubDependencyIssueDto>(result.StdoutText, JsonOptions);
        return dependency with
        {
            Status = string.Equals(issue?.State, "closed", StringComparison.OrdinalIgnoreCase)
                ? WorkItemDependencyStatus.Satisfied
                : string.Equals(issue?.State, "open", StringComparison.OrdinalIgnoreCase)
                    ? WorkItemDependencyStatus.Unsatisfied
                    : WorkItemDependencyStatus.Unknown,
        };
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private sealed class GitHubDependencyIssueDto
    {
        public string? State { get; set; }
    }
}

