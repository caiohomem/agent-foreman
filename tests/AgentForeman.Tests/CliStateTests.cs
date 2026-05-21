using AgentForeman.Cli;
using AgentForeman.Core.Configuration;
using AgentForeman.Core.Prerequisites;
using AgentForeman.Core.State;

namespace AgentForeman.Tests;

public sealed class CliStateTests
{
    [Fact]
    public void StateInitCommandParsingWorks()
    {
        var stateStore = new FakeStateStore();
        var services = DoctorTestServices.Valid();

        var result = CliApplication.Execute(
            new[] { "state", "init" },
            services.ConfigLoader,
            services.RepositoryChecker,
            services.CommandChecker,
            stateStore);

        Assert.Equal(0, result.ExitCode);
        Assert.True(stateStore.InitializeCalled);
        Assert.Equal(
            """
            State database initialized.
            Provider: postgresql

            """,
            result.Output);
    }

    [Fact]
    public void StateStatusCommandParsingWorks()
    {
        var stateStore = new FakeStateStore(new StateStoreStatus("postgresql", MissionCount: 3, ProviderStateCount: 2));
        var services = DoctorTestServices.Valid();

        var result = CliApplication.Execute(
            new[] { "state", "status" },
            services.ConfigLoader,
            services.RepositoryChecker,
            services.CommandChecker,
            stateStore);

        Assert.Equal(0, result.ExitCode);
        Assert.Equal(
            """
            State database status
            Provider: postgresql
            Missions: 3
            Provider states: 2

            """,
            result.Output);
    }
}

internal sealed class FakeStateStore : IStateStore
{
    private readonly StateStoreStatus _status;

    public FakeStateStore()
        : this(new StateStoreStatus("postgresql", MissionCount: 0, ProviderStateCount: 0))
    {
    }

    public FakeStateStore(StateStoreStatus status)
    {
        _status = status;
    }

    public bool InitializeCalled { get; private set; }

    public void Initialize(AgentForemanConfig config)
    {
        InitializeCalled = true;
    }

    public StateStoreStatus GetStatus(AgentForemanConfig config)
    {
        return _status;
    }
}
