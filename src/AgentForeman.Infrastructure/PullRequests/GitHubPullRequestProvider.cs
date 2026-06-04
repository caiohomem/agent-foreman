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

    public async Task<PullRequestResult> EnableAutoMergeAsync(
        PullRequestAutoMergeRequest request,
        CancellationToken cancellationToken)
    {
        var startedAt = DateTimeOffset.UtcNow;
        var prNumber = ExtractPullRequestNumber(request.PullRequestUrl);
        var arguments = new List<string>
        {
            "pr", "merge", prNumber,
            "--repo", request.Repository,
            "--auto",
        };
        var method = (request.Method ?? string.Empty).Trim().ToLowerInvariant();
        arguments.Add(method switch
        {
            "" or "squash" => "--squash",
            "merge" => "--merge",
            "rebase" => "--rebase",
            _ => "--squash",
        });

        var result = await _commandRunner.RunAsync(
            new CommandRequest("gh", arguments.ToArray()),
            cancellationToken: cancellationToken);

        var finishedAt = DateTimeOffset.UtcNow;
        if (result.Success)
        {
            return new PullRequestResult(
                true, request.PullRequestUrl, result.StdoutText, result.StderrText,
                result.ExitCode, startedAt, finishedAt, null);
        }

        return new PullRequestResult(
            false, request.PullRequestUrl, result.StdoutText, result.StderrText,
            result.ExitCode, startedAt, finishedAt,
            string.IsNullOrWhiteSpace(result.StderrText)
                ? "gh pr merge --auto failed."
                : result.StderrText.Trim());
    }

    private static string ExtractPullRequestNumber(string pullRequestUrl)
    {
        var trimmed = (pullRequestUrl ?? string.Empty).Trim().TrimEnd('/');
        var lastSegment = trimmed[(trimmed.LastIndexOf('/') + 1)..];
        return int.TryParse(lastSegment, out _) ? lastSegment : trimmed;
    }
}
