using AgentForeman.Core.Commands;
using AgentForeman.Core.PullRequests;

namespace AgentForeman.Infrastructure.PullRequests;

public sealed class GitHubPullRequestProvider : IPullRequestProvider
{
    private readonly ICommandRunner _commandRunner;

    public GitHubPullRequestProvider(ICommandRunner commandRunner)
    {
        _commandRunner = commandRunner;
    }

    public async Task<PullRequestResult> CreateAsync(PullRequestRequest request, CancellationToken cancellationToken)
    {
        var startedAt = DateTimeOffset.UtcNow;
        var result = await _commandRunner.RunAsync(
            new CommandRequest("gh", new[]
            {
                "pr", "create",
                "--repo", request.Repository,
                "--title", request.PullRequestTitle,
                "--body", request.PullRequestBody,
                "--base", request.BaseBranch,
                "--head", request.Branch,
            }),
            cancellationToken: cancellationToken);

        var finishedAt = DateTimeOffset.UtcNow;
        var url = result.StdoutText.Trim();

        if (result.Success)
        {
            return new PullRequestResult(
                true, url, result.StdoutText, result.StderrText,
                result.ExitCode, startedAt, finishedAt, null);
        }

        return new PullRequestResult(
            false, null, result.StdoutText, result.StderrText,
            result.ExitCode, startedAt, finishedAt,
            string.IsNullOrWhiteSpace(result.StderrText) ? "gh pr create failed." : result.StderrText.Trim());
    }
}
