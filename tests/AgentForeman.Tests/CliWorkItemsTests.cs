using AgentForeman.Cli;
using AgentForeman.Core.Commands;
using AgentForeman.Core.WorkItems;

namespace AgentForeman.Tests;

public sealed class CliWorkItemsTests
{
    [Fact]
    public void WorkItemsReadyListsReadyItems()
    {
        var services = DoctorTestServices.Valid();
        var runner = new RecordingCommandRunner(
            """
            [
              {
                "number": 42,
                "title": "Fix elevator ad pacing",
                "body": "Issue body",
                "url": "https://github.com/caio/elevator-ads-mvp/issues/42",
                "labels": [{"name": "agent-ready"}],
                "createdAt": "2026-05-01T10:00:00Z",
                "updatedAt": "2026-05-02T10:00:00Z"
              }
            ]
            """);

        var result = CliApplication.Execute(
            new[] { "work-items", "ready" },
            services.ConfigLoader,
            services.RepositoryChecker,
            services.CommandChecker,
            new FakeStateStore(),
            runner);

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("#42 Fix elevator ad pacing", result.Output);
        Assert.Contains("https://github.com/caio/elevator-ads-mvp/issues/42", result.Output);
    }

    [Fact]
    public void WorkItemsViewPrintsIssueDetails()
    {
        var services = DoctorTestServices.Valid();
        var runner = new RecordingCommandRunner(
            """
            {
              "number": 42,
              "title": "Fix elevator ad pacing",
              "body": "Issue body",
              "url": "https://github.com/caio/elevator-ads-mvp/issues/42",
              "labels": [{"name": "agent-ready"}],
              "createdAt": "2026-05-01T10:00:00Z",
              "updatedAt": "2026-05-02T10:00:00Z"
            }
            """);

        var result = CliApplication.Execute(
            new[] { "work-items", "view", "42" },
            services.ConfigLoader,
            services.RepositoryChecker,
            services.CommandChecker,
            new FakeStateStore(),
            runner);

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("#42 Fix elevator ad pacing", result.Output);
        Assert.Contains("Labels: agent-ready", result.Output);
        Assert.Contains("Issue body", result.Output);
    }
}
