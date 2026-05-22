namespace AgentForeman.Core.Testing;

public interface ITestRunner
{
    Task<TestRunResult> RunAsync(TestRunRequest request, CancellationToken cancellationToken);
}
