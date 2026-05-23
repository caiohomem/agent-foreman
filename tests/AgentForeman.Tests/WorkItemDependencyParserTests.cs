using AgentForeman.Core.WorkItems;
using AgentForeman.Infrastructure.WorkItems;

namespace AgentForeman.Tests;

public sealed class WorkItemDependencyParserTests
{
    private readonly IWorkItemDependencyParser _parser = new WorkItemDependencyParser();

    [Fact]
    public void ParsesSingleDependency()
    {
        var dependencies = _parser.Parse("Depends on: #1", "caio/elevator-ads-mvp");

        var dependency = Assert.Single(dependencies);
        Assert.Equal("1", dependency.Reference);
        Assert.Equal("caio/elevator-ads-mvp", dependency.Repository);
    }

    [Fact]
    public void ParsesMultipleDependencies()
    {
        var dependencies = _parser.Parse("Depends on: #1, #2", "caio/elevator-ads-mvp");

        Assert.Collection(
            dependencies,
            dependency => Assert.Equal("1", dependency.Reference),
            dependency => Assert.Equal("2", dependency.Reference));
    }

    [Fact]
    public void ParsesLowercaseDependencies()
    {
        var dependencies = _parser.Parse("depends on: #1", "caio/elevator-ads-mvp");

        var dependency = Assert.Single(dependencies);
        Assert.Equal("1", dependency.Reference);
    }

    [Fact]
    public void IgnoresBodiesWithoutDependencies()
    {
        var dependencies = _parser.Parse("No blockers here", "caio/elevator-ads-mvp");

        Assert.Empty(dependencies);
    }
}

