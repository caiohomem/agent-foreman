using AgentForeman.Core.Configuration;
using AgentForeman.Core.WorkItems;
using AgentForeman.Infrastructure.WorkItems;

namespace AgentForeman.Tests;

public sealed class GitHubWorkItemDependencyResolverTests
{
    [Fact]
    public async Task MarksClosedDependencyAsSatisfied()
    {
        var runner = new RecordingCommandRunner(
            """
            {
              "number": 1,
              "state": "CLOSED",
              "title": "Base work",
              "url": "https://github.com/caio/elevator-ads-mvp/issues/1"
            }
            """);
        var resolver = new GitHubWorkItemDependencyResolver(Config(), runner);
        var dependency = new WorkItemDependency("1", "caio/elevator-ads-mvp");

        var result = await resolver.ResolveAsync(dependency, CancellationToken.None);

        Assert.Equal(WorkItemDependencyStatus.Satisfied, result.Status);
    }

    [Fact]
    public async Task MarksOpenDependencyAsUnsatisfied()
    {
        var runner = new RecordingCommandRunner(
            """
            {
              "number": 1,
              "state": "OPEN",
              "title": "Base work",
              "labels": [{"name": "agent-review"}],
              "url": "https://github.com/caio/elevator-ads-mvp/issues/1"
            }
            """);
        var resolver = new GitHubWorkItemDependencyResolver(Config(), runner);
        var dependency = new WorkItemDependency("1", "caio/elevator-ads-mvp");

        var result = await resolver.ResolveAsync(dependency, CancellationToken.None);

        Assert.Equal(WorkItemDependencyStatus.Unsatisfied, result.Status);
    }

    private static AgentForemanConfig Config()
    {
        return new AgentForemanConfig
        {
            WorkItems = new WorkItemsConfig
            {
                Provider = "github",
                Repo = "caio/elevator-ads-mvp",
            },
        };
    }
}
