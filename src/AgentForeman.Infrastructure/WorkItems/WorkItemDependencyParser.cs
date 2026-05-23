using System.Text.RegularExpressions;
using AgentForeman.Core.WorkItems;

namespace AgentForeman.Infrastructure.WorkItems;

public sealed partial class WorkItemDependencyParser : IWorkItemDependencyParser
{
    public IReadOnlyList<WorkItemDependency> Parse(string body, string repository)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return Array.Empty<WorkItemDependency>();
        }

        var dependencies = new List<WorkItemDependency>();
        foreach (var line in body.Split(["\r\n", "\n"], StringSplitOptions.None))
        {
            var match = DependsOnLineRegex().Match(line);
            if (!match.Success)
            {
                continue;
            }

            var values = match.Groups["values"].Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var value in values)
            {
                var dependencyMatch = DependencyRegex().Match(value);
                if (!dependencyMatch.Success)
                {
                    continue;
                }

                var dependencyRepository = dependencyMatch.Groups["repo"].Success
                    ? dependencyMatch.Groups["repo"].Value
                    : repository;
                dependencies.Add(new WorkItemDependency(dependencyMatch.Groups["number"].Value, dependencyRepository));
            }
        }

        return dependencies;
    }

    [GeneratedRegex(@"^\s*depends on:\s*(?<values>.+)\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex DependsOnLineRegex();

    [GeneratedRegex(@"^(?:(?<repo>[\w.-]+/[\w.-]+))?#(?<number>\d+)$", RegexOptions.IgnoreCase)]
    private static partial Regex DependencyRegex();
}

