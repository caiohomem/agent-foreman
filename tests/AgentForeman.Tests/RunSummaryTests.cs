using AgentForeman.Cli;
using AgentForeman.Core.Commands;
using AgentForeman.Core.Configuration;
using AgentForeman.Core.Git;
using AgentForeman.Core.PullRequests;
using AgentForeman.Core.Safety;
using AgentForeman.Core.State;
using AgentForeman.Core.Summaries;
using AgentForeman.Core.Testing;
using AgentForeman.Infrastructure.State;
using AgentForeman.Infrastructure.Summaries;

namespace AgentForeman.Tests;

public sealed class RunSummaryTests
{
    [Fact]
    public void SummaryPathsAreGeneratedCorrectly()
    {
        var outputDirectory = Path.Combine("/workspace/project", ".agent", "runs", "issue-42");

        Assert.Equal(Path.Combine(outputDirectory, "summary.md"), RunSummaryPaths.GetPath(outputDirectory, RunSummaryType.SuccessSummary));
        Assert.Equal(Path.Combine(outputDirectory, "failure-summary.md"), RunSummaryPaths.GetPath(outputDirectory, RunSummaryType.FailureSummary));
        Assert.Equal(Path.Combine(outputDirectory, "resume-context.md"), RunSummaryPaths.GetPath(outputDirectory, RunSummaryType.ResumeContext));
    }

    [Fact]
    public async Task SummaryIsSavedToDatabase()
    {
        using var workspace = TemporaryDirectory.Create();
        Directory.CreateDirectory(workspace.Path);
        var outputDirectory = Path.Combine(workspace.Path, ".agent", "runs", "issue-42");
        Directory.CreateDirectory(outputDirectory);
        File.WriteAllText(Path.Combine(outputDirectory, "plan.md"), "# Plan");

        var repository = new FakeRunSummaryRepository();
        var generator = new FakeRunSummaryGenerator("## Summary");
        var git = new FakeGitRepository { Diff = "diff --git a/app.cs b/app.cs" };
        var service = new RunSummaryService(generator, repository, git);
        var mission = new Mission(
            "github-42",
            "42",
            "GitHub",
            "Fix pacing",
            MissionStatus.PullRequestCreated,
            "agent/issue-42",
            Path.Combine(outputDirectory, "plan.md"),
            "https://github.com/example/repo/pull/42",
            null,
            null,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);

        var summaries = await service.GenerateAndSaveAsync(
            new RunSummaryGenerationRequest(
                mission,
                workspace.Path,
                outputDirectory,
                "Fix pacing",
                "Issue body",
                MissionStatus.PullRequestCreated,
                [RunSummaryType.SuccessSummary],
                mission.PullRequestUrl),
            CancellationToken.None);

        Assert.Single(summaries);
        Assert.Single(repository.Saved);
        Assert.Equal(RunSummaryType.SuccessSummary, repository.Saved[0].SummaryType);
        Assert.Equal(Path.Combine(outputDirectory, "summary.md"), repository.Saved[0].Path);
        Assert.True(File.Exists(repository.Saved[0].Path!));
    }

    [Fact]
    public async Task ClaudeSummaryGeneratorWritesContextFileInsideRunDirectory()
    {
        using var workspace = TemporaryDirectory.Create();
        Directory.CreateDirectory(workspace.Path);
        var outputDirectory = Path.Combine(workspace.Path, ".agent", "runs", "issue-42");
        Directory.CreateDirectory(outputDirectory);

        var runner = new RecordingCommandRunner("## Summary");
        var generator = new ClaudeCliRunSummaryGenerator(runner, "claude");

        await generator.GenerateAsync(
            RunSummaryType.SuccessSummary,
            new RunSummaryRequest(
                "github-42",
                "42",
                outputDirectory,
                workspace.Path,
                "Fix pacing",
                "Issue body",
                "# Plan",
                "codex log",
                "tests log",
                Array.Empty<RunSummaryArtifact>(),
                "diff --git a/app.cs b/app.cs",
                "https://github.com/example/repo/pull/42",
                MissionStatus.PullRequestCreated.ToString()),
            CancellationToken.None);

        Assert.NotNull(runner.LastRequest);
        Assert.Equal(workspace.Path, runner.LastRequest!.WorkingDirectory);
        Assert.Contains(outputDirectory, runner.LastRequest.Arguments[1]);
        Assert.DoesNotContain("/tmp/agent-foreman-run-summaries", runner.LastRequest.Arguments[1], StringComparison.Ordinal);
        Assert.True(File.Exists(Path.Combine(outputDirectory, "summary-context-successsummary.md")));
    }

    [Fact]
    public async Task ClaudeSummaryGeneratorRunsFromRepoPathSoContextFileIsReadable()
    {
        using var workspace = TemporaryDirectory.Create();
        Directory.CreateDirectory(workspace.Path);
        var outputDirectory = Path.Combine(workspace.Path, ".agent", "runs", "issue-42");
        Directory.CreateDirectory(outputDirectory);

        var runner = new RecordingCommandRunner("## Summary");
        var generator = new ClaudeCliRunSummaryGenerator(runner, "claude");

        await generator.GenerateAsync(
            RunSummaryType.SuccessSummary,
            new RunSummaryRequest(
                "github-42",
                "42",
                outputDirectory,
                workspace.Path,
                "Fix pacing",
                "Issue body",
                "# Plan",
                "codex log",
                "tests log",
                Array.Empty<RunSummaryArtifact>(),
                "diff --git a/app.cs b/app.cs",
                "https://github.com/example/repo/pull/42",
                MissionStatus.PullRequestCreated.ToString()),
            CancellationToken.None);

        Assert.NotNull(runner.LastRequest);
        Assert.Equal(workspace.Path, runner.LastRequest!.WorkingDirectory);
        Assert.Equal("claude", runner.LastRequest.Executable);
        Assert.Equal("--print", runner.LastRequest.Arguments[0]);
    }

    [Fact]
    public void SummarizeCommandReadsPlanAndLogFiles()
    {
        using var workspace = TemporaryDirectory.Create();
        Directory.CreateDirectory(workspace.Path);
        var outputDirectory = Path.Combine(workspace.Path, ".agent", "runs", "issue-42");
        Directory.CreateDirectory(outputDirectory);
        File.WriteAllText(Path.Combine(outputDirectory, "plan.md"), "# Plan");
        File.WriteAllText(Path.Combine(outputDirectory, "codex-exec.log"), "codex log");
        File.WriteAllText(Path.Combine(outputDirectory, "tests.log"), "tests log");
        File.WriteAllText(Path.Combine(outputDirectory, "repair-attempt-1.log"), "repair log");

        var configLoader = new FakeConfigLoader(AgentForemanConfigLoadResult.Success(CreateConfig(workspace.Path)));
        var missions = new FakeMissionRepository();
        missions.Save(new Mission(
            "github-42",
            "42",
            "GitHub",
            "Fix pacing",
            MissionStatus.Failed,
            "agent/issue-42",
            Path.Combine(outputDirectory, "plan.md"),
            null,
            null,
            "tests failed",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow));
        var workItems = new FakeWorkItemProvider(true);
        var summaryRepository = new FakeRunSummaryRepository();
        var summaryGenerator = new FakeRunSummaryGenerator("## Failure");
        var git = new FakeGitRepository { Diff = "diff --git a/src/app.cs b/src/app.cs" };

        var result = CliApplication.Execute(
            ["summarize", "github-42"],
            configLoader,
            new FakeRepositoryChecker(true, true),
            new FakeCommandAvailabilityChecker(null),
            new FakeStateStore(),
            new FakeCommandRunner(),
            gitRepository: git,
            workItemProvider: workItems,
            missionRepository: missions,
            runSummaryRepository: summaryRepository,
            runSummaryGenerator: summaryGenerator);

        Assert.Equal(0, result.ExitCode);
        Assert.NotNull(summaryGenerator.LastRequest);
        Assert.Equal("# Plan", summaryGenerator.LastRequest!.PlanContent);
        Assert.Equal("codex log", summaryGenerator.LastRequest.ExecutionLogContent);
        Assert.Equal("tests log", summaryGenerator.LastRequest.TestsLogContent);
        Assert.Single(summaryGenerator.LastRequest.RepairLogs);
        Assert.Equal("diff --git a/src/app.cs b/src/app.cs", summaryGenerator.LastRequest.CurrentGitDiff);
        Assert.Contains("failure-summary.md", result.Output);
        Assert.Contains("resume-context.md", result.Output);
    }

    [Fact]
    public void SummarizeCommandAcceptsExternalWorkItemId()
    {
        using var workspace = TemporaryDirectory.Create();
        Directory.CreateDirectory(workspace.Path);
        var outputDirectory = Path.Combine(workspace.Path, ".agent", "runs", "issue-24");
        Directory.CreateDirectory(outputDirectory);
        File.WriteAllText(Path.Combine(outputDirectory, "plan.md"), "# Plan");

        var configLoader = new FakeConfigLoader(AgentForemanConfigLoadResult.Success(CreateConfig(workspace.Path)));
        var missions = new FakeMissionRepository();
        missions.Save(new Mission(
            "github-24",
            "24",
            "GitHub",
            "Add summary generation",
            MissionStatus.PullRequestCreated,
            "agent/issue-24",
            Path.Combine(outputDirectory, "plan.md"),
            null,
            null,
            null,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow));

        var result = CliApplication.Execute(
            ["summarize", "24"],
            configLoader,
            new FakeRepositoryChecker(true, true),
            new FakeCommandAvailabilityChecker(null),
            new FakeStateStore(),
            new FakeCommandRunner(),
            gitRepository: new FakeGitRepository(),
            workItemProvider: new FakeWorkItemProvider(true),
            missionRepository: missions,
            runSummaryRepository: new FakeRunSummaryRepository(),
            runSummaryGenerator: new FakeRunSummaryGenerator("## Success"));

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("summary.md", result.Output);
    }

    [Fact]
    public void SummarizeCommandHandlesMissingOptionalLogs()
    {
        using var workspace = TemporaryDirectory.Create();
        Directory.CreateDirectory(workspace.Path);
        var outputDirectory = Path.Combine(workspace.Path, ".agent", "runs", "issue-42");
        Directory.CreateDirectory(outputDirectory);
        File.WriteAllText(Path.Combine(outputDirectory, "plan.md"), "# Plan");

        var configLoader = new FakeConfigLoader(AgentForemanConfigLoadResult.Success(CreateConfig(workspace.Path)));
        var missions = new FakeMissionRepository();
        missions.Save(new Mission(
            "github-42",
            "42",
            "GitHub",
            "Fix pacing",
            MissionStatus.PullRequestCreated,
            "agent/issue-42",
            Path.Combine(outputDirectory, "plan.md"),
            "https://github.com/example/repo/pull/42",
            null,
            null,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow));
        var summaryRepository = new FakeRunSummaryRepository();
        var summaryGenerator = new FakeRunSummaryGenerator("## Success");

        var result = CliApplication.Execute(
            ["summarize", "github-42"],
            configLoader,
            new FakeRepositoryChecker(true, true),
            new FakeCommandAvailabilityChecker(null),
            new FakeStateStore(),
            new FakeCommandRunner(),
            gitRepository: new FakeGitRepository(),
            workItemProvider: new FakeWorkItemProvider(true),
            missionRepository: missions,
            runSummaryRepository: summaryRepository,
            runSummaryGenerator: summaryGenerator);

        Assert.Equal(0, result.ExitCode);
        Assert.NotNull(summaryGenerator.LastRequest);
        Assert.Null(summaryGenerator.LastRequest!.ExecutionLogContent);
        Assert.Null(summaryGenerator.LastRequest.TestsLogContent);
        Assert.Empty(summaryGenerator.LastRequest.RepairLogs);
    }

    [Fact]
    public void SuccessfulMissionCreatesSuccessSummary()
    {
        using var workspace = TemporaryDirectory.Create();
        Directory.CreateDirectory(workspace.Path);
        var outputDirectory = Path.Combine(workspace.Path, ".agent", "runs", "issue-42");
        Directory.CreateDirectory(outputDirectory);
        File.WriteAllText(Path.Combine(outputDirectory, "plan.md"), "# Plan");
        File.WriteAllText(Path.Combine(outputDirectory, "codex-exec.log"), "codex log");
        File.WriteAllText(Path.Combine(outputDirectory, "tests.log"), "tests log");

        var configLoader = new FakeConfigLoader(AgentForemanConfigLoadResult.Success(CreateConfig(workspace.Path)));
        var missions = new FakeMissionRepository();
        missions.Save(new Mission(
            "github-42",
            "42",
            "GitHub",
            "Fix pacing",
            MissionStatus.TestsPassed,
            null,
            Path.Combine(outputDirectory, "plan.md"),
            null,
            null,
            null,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow));

        var summaryRepository = new FakeRunSummaryRepository();
        var result = CliApplication.Execute(
            ["submit", "42"],
            configLoader,
            new FakeRepositoryChecker(true, true),
            new FakeCommandAvailabilityChecker(null),
            new FakeStateStore(),
            new RecordingCommandRunner(),
            gitRepository: new RecordingGitRepository(currentBranch: "agent/issue-42"),
            workItemProvider: new FakeWorkItemProvider(true),
            missionRepository: missions,
            pullRequestProvider: new FakePullRequestProvider(),
            missionEventRecorder: new FakeMissionEventRecorder(),
            runSummaryRepository: summaryRepository,
            runSummaryGenerator: new FakeRunSummaryGenerator("## Success"));

        Assert.Equal(0, result.ExitCode);
        Assert.Single(summaryRepository.Saved);
        Assert.Equal(RunSummaryType.SuccessSummary, summaryRepository.Saved[0].SummaryType);
        Assert.Contains("summary.md", result.Output);
    }

    [Fact]
    public void FailedMissionCreatesFailureSummaryAndResumeContext()
    {
        using var workspace = TemporaryDirectory.Create();
        Directory.CreateDirectory(workspace.Path);
        var outputDirectory = Path.Combine(workspace.Path, ".agent", "runs", "issue-42");
        Directory.CreateDirectory(outputDirectory);
        File.WriteAllText(Path.Combine(outputDirectory, "plan.md"), "# Plan");

        var configLoader = new FakeConfigLoader(AgentForemanConfigLoadResult.Success(CreateConfig(workspace.Path)));
        var missions = new FakeMissionRepository();
        var summaryRepository = new FakeRunSummaryRepository();
        var testRunner = new FakeTestRunner(new TestRunResult(
            false,
            Path.Combine(outputDirectory, "tests.log"),
            [new TestCommandResult("dotnet test", 1, "", "failed", TimeSpan.FromSeconds(1))],
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            "tests failed"));

        var result = CliApplication.Execute(
            ["verify", "42"],
            configLoader,
            new FakeRepositoryChecker(true, true),
            new FakeCommandAvailabilityChecker(null),
            new FakeStateStore(),
            new RecordingCommandRunner(),
            gitRepository: new FakeGitRepository(),
            workItemProvider: new FakeWorkItemProvider(true),
            missionRepository: missions,
            testRunner: testRunner,
            safetyChecker: new FakeSafetyChecker(new SafetyCheckResult(true, Array.Empty<SafetyViolation>())),
            missionEventRecorder: new FakeMissionEventRecorder(),
            runSummaryRepository: summaryRepository,
            runSummaryGenerator: new FakeRunSummaryGenerator("## Failure"));

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains(summaryRepository.Saved, summary => summary.SummaryType == RunSummaryType.FailureSummary);
        Assert.Contains(summaryRepository.Saved, summary => summary.SummaryType == RunSummaryType.ResumeContext);
    }

    private static AgentForemanConfig CreateConfig(string repoPath)
    {
        return new AgentForemanConfig
        {
            Project = new ProjectConfig
            {
                Name = "project",
                RepoPath = repoPath,
                DefaultBranch = "main",
            },
            WorkItems = new WorkItemsConfig
            {
                Provider = "github",
                Repo = "caio/elevator-ads-mvp",
                ReviewLabel = "agent-review",
                WorkingLabel = "agent-working",
                ReadyLabel = "agent-ready",
            },
            Planner = new PlannerConfig
            {
                Provider = "claude-cli",
                Command = "claude",
            },
            Executor = new ExecutorConfig
            {
                Provider = "codex-cli",
                Command = "codex",
                Approval = "never",
                Sandbox = "workspace-write",
            },
            Database = new DatabaseConfig
            {
                Provider = "postgresql",
                ConnectionString = "Host=localhost;Database=agent_foreman",
            },
            Tests = new TestsConfig
            {
                Commands = ["dotnet test"],
            },
            Safety = new SafetyConfig(),
        };
    }
}

internal sealed class FakeRunSummaryRepository : IRunSummaryRepository
{
    public List<RunSummary> Saved { get; } = new();

    public Task SaveRunSummaryAsync(RunSummary runSummary, CancellationToken cancellationToken)
    {
        Saved.Add(runSummary);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<RunSummary>> GetRunSummariesAsync(string missionId, CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<RunSummary>>(Saved.Where(summary => summary.MissionId == missionId).ToList());
    }

    public Task<RunSummary?> GetLatestRunSummaryAsync(string missionId, RunSummaryType? summaryType, CancellationToken cancellationToken)
    {
        var summary = Saved
            .Where(item => item.MissionId == missionId && (summaryType is null || item.SummaryType == summaryType))
            .OrderByDescending(item => item.CreatedAt)
            .FirstOrDefault();
        return Task.FromResult(summary);
    }
}

internal sealed class FakeRunSummaryGenerator : IRunSummaryGenerator
{
    private readonly string _content;

    public FakeRunSummaryGenerator(string content)
    {
        _content = content;
    }

    public List<(RunSummaryType Type, RunSummaryRequest Request)> Requests { get; } = new();
    public RunSummaryRequest? LastRequest => Requests.LastOrDefault().Request;

    public Task<RunSummaryResult> GenerateAsync(RunSummaryType summaryType, RunSummaryRequest request, CancellationToken cancellationToken)
    {
        Requests.Add((summaryType, request));
        return Task.FromResult(new RunSummaryResult(summaryType, _content));
    }
}
