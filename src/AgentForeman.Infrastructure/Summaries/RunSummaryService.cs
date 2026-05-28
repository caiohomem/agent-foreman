using AgentForeman.Core.Git;
using AgentForeman.Core.State;
using AgentForeman.Core.Summaries;

namespace AgentForeman.Infrastructure.Summaries;

public sealed class RunSummaryService
{
    private readonly IRunSummaryGenerator _generator;
    private readonly IRunSummaryRepository _repository;
    private readonly IGitRepository _gitRepository;

    public RunSummaryService(
        IRunSummaryGenerator generator,
        IRunSummaryRepository repository,
        IGitRepository gitRepository)
    {
        _generator = generator;
        _repository = repository;
        _gitRepository = gitRepository;
    }

    public async Task<IReadOnlyList<RunSummary>> GenerateAndSaveAsync(
        RunSummaryGenerationRequest request,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(request.OutputDirectory);

        var summaryRequest = await BuildRequestAsync(request, cancellationToken);
        var summaries = new List<RunSummary>(request.SummaryTypes.Count);

        foreach (var summaryType in request.SummaryTypes)
        {
            var generated = await _generator.GenerateAsync(summaryType, summaryRequest, cancellationToken);
            var path = RunSummaryPaths.GetPath(request.OutputDirectory, summaryType);
            await File.WriteAllTextAsync(path, generated.Content, cancellationToken);

            var runSummary = new RunSummary(
                Guid.NewGuid().ToString("N"),
                request.Mission.Id,
                request.Mission.ExternalWorkItemId,
                summaryType,
                generated.Content,
                path,
                DateTimeOffset.UtcNow);

            await _repository.SaveRunSummaryAsync(runSummary, cancellationToken);
            summaries.Add(runSummary);
        }

        return summaries;
    }

    private async Task<RunSummaryRequest> BuildRequestAsync(
        RunSummaryGenerationRequest request,
        CancellationToken cancellationToken)
    {
        var planPath = request.Mission.PlanPath ?? Path.Combine(request.OutputDirectory, "plan.md");
        var planContent = ReadIfExists(planPath);
        var executionLogContent = ReadIfExists(Path.Combine(request.OutputDirectory, "codex-exec.log"));
        var testsLogContent = ReadIfExists(Path.Combine(request.OutputDirectory, "tests.log"));
        var repairLogs = ReadRepairLogs(request.OutputDirectory);
        var currentDiff = request.CurrentGitDiff;

        if (currentDiff is null)
        {
            try
            {
                currentDiff = await _gitRepository.GetDiffAsync(request.RepoPath, cancellationToken);
            }
            catch
            {
                currentDiff = null;
            }
        }

        if (string.IsNullOrWhiteSpace(currentDiff))
        {
            currentDiff = null;
        }

        return new RunSummaryRequest(
            request.Mission.Id,
            request.Mission.ExternalWorkItemId,
            request.OutputDirectory,
            request.IssueTitle,
            request.IssueBody,
            planContent,
            executionLogContent,
            testsLogContent,
            repairLogs,
            currentDiff,
            request.PullRequestUrl ?? request.Mission.PullRequestUrl,
            request.FinalMissionStatus.ToString());
    }

    private static string? ReadIfExists(string path)
    {
        return File.Exists(path) ? File.ReadAllText(path) : null;
    }

    private static IReadOnlyList<RunSummaryArtifact> ReadRepairLogs(string outputDirectory)
    {
        if (!Directory.Exists(outputDirectory))
        {
            return Array.Empty<RunSummaryArtifact>();
        }

        return Directory
            .EnumerateFiles(outputDirectory, "*repair*.log", SearchOption.TopDirectoryOnly)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .Select(path => new RunSummaryArtifact(path, File.ReadAllText(path)))
            .ToList();
    }
}
