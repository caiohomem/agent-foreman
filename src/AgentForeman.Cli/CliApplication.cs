using CoreCommandOutputLine = AgentForeman.Core.Commands.CommandOutputLine;
using CoreCommandOutputStream = AgentForeman.Core.Commands.CommandOutputStream;
using CoreCommandRequest = AgentForeman.Core.Commands.CommandRequest;
using CoreICommandRunner = AgentForeman.Core.Commands.ICommandRunner;
using System.Text;
using AgentForeman.Core.Coding;
using AgentForeman.Core.Configuration;
using AgentForeman.Core.Events;
using AgentForeman.Core.Orchestration;
using AgentForeman.Core.PullRequests;
using AgentForeman.Core.Safety;
using AgentForeman.Core.Testing;
using AgentForeman.Core.Git;
using AgentForeman.Core.Labels;
using AgentForeman.Core.Planning;
using AgentForeman.Core.Prerequisites;
using AgentForeman.Core.State;
using AgentForeman.Core.Summaries;
using AgentForeman.Core.WorkItems;
using AgentForeman.Infrastructure.Coding;
using AgentForeman.Infrastructure.Commands;
using AgentForeman.Infrastructure.Orchestration;
using AgentForeman.Infrastructure.PullRequests;
using AgentForeman.Infrastructure.Safety;
using AgentForeman.Infrastructure.Testing;
using AgentForeman.Infrastructure.Configuration;
using AgentForeman.Infrastructure.Git;
using AgentForeman.Infrastructure.Labels;
using AgentForeman.Infrastructure.Planning;
using AgentForeman.Infrastructure.Prerequisites;
using AgentForeman.Infrastructure.State;
using AgentForeman.Infrastructure.Summaries;
using AgentForeman.Infrastructure.WorkItems;

namespace AgentForeman.Cli;

public static class CliApplication
{
    public static CommandResult Execute(IReadOnlyList<string> args)
    {
        return Execute(
            args,
            new YamlAgentForemanConfigLoader(),
            new FileSystemRepositoryChecker(),
            new PathCommandAvailabilityChecker(),
            new PostgresStateStore(),
            new ProcessCommandRunner(),
            WriteExecOutputLine,
            null);
    }

    public static CommandResult Execute(IReadOnlyList<string> args, IAgentForemanConfigLoader configLoader)
    {
        return Execute(
            args,
            configLoader,
            new FileSystemRepositoryChecker(),
            new PathCommandAvailabilityChecker(),
            new PostgresStateStore(),
            new ProcessCommandRunner(),
            onExecOutputLine: null,
            gitRepository: null);
    }

    public static CommandResult Execute(
        IReadOnlyList<string> args,
        IAgentForemanConfigLoader configLoader,
        IRepositoryChecker repositoryChecker,
        ICommandAvailabilityChecker commandChecker)
    {
        return Execute(
            args,
            configLoader,
            repositoryChecker,
            commandChecker,
            new PostgresStateStore(),
            new ProcessCommandRunner(),
            onExecOutputLine: null,
            gitRepository: null);
    }

    public static CommandResult Execute(
        IReadOnlyList<string> args,
        IAgentForemanConfigLoader configLoader,
        IRepositoryChecker repositoryChecker,
        ICommandAvailabilityChecker commandChecker,
        IStateStore stateStore)
    {
        return Execute(
            args,
            configLoader,
            repositoryChecker,
            commandChecker,
            stateStore,
            new ProcessCommandRunner(),
            onExecOutputLine: null,
            gitRepository: null);
    }

    public static CommandResult Execute(
        IReadOnlyList<string> args,
        IAgentForemanConfigLoader configLoader,
        IRepositoryChecker repositoryChecker,
        ICommandAvailabilityChecker commandChecker,
        IStateStore stateStore,
        CoreICommandRunner commandRunner,
        Action<CoreCommandOutputLine>? onExecOutputLine = null,
        IGitRepository? gitRepository = null,
        IWorkItemProvider? workItemProvider = null,
        IPlanningAgent? planningAgent = null,
        IMissionRepository? missionRepository = null,
        ICodingAgent? codingAgent = null,
        ITestRunner? testRunner = null,
        ISafetyChecker? safetyChecker = null,
        IPullRequestProvider? pullRequestProvider = null,
        IMissionOrchestrator? orchestrator = null,
        IMissionBranchPreparer? branchPreparer = null,
        IWorkItemDependencyResolver? dependencyResolver = null,
        ILabelManager? labelManager = null,
        IMissionEventRecorder? missionEventRecorder = null,
        IRunSummaryRepository? runSummaryRepository = null,
        IRunSummaryGenerator? runSummaryGenerator = null)
    {
        gitRepository ??= new CliGitRepository(commandRunner);

        if (args.Count == 0 || IsHelpArgument(args[0]))
        {
            return new CommandResult(0, HelpText.Value, string.Empty);
        }

        if (IsConfigValidateCommand(args))
        {
            return ValidateConfig(args, configLoader);
        }

        if (args[0] == "doctor")
        {
            return RunDoctor(args, configLoader, repositoryChecker, commandChecker);
        }

        if (args[0] == "state")
        {
            return RunStateCommand(args, configLoader, stateStore);
        }

        if (args[0] == "exec")
        {
            return RunExecCommand(args, commandRunner, onExecOutputLine);
        }

        if (args[0] == "git")
        {
            return RunGitCommand(args, configLoader, gitRepository);
        }

        if (args[0] == "labels")
        {
            return RunLabelsCommand(args, configLoader, commandRunner, labelManager);
        }

        if (args[0] == "work-items")
        {
            return RunWorkItemsCommand(args, configLoader, commandRunner, dependencyResolver);
        }

        if (args[0] == "plan")
        {
            return RunPlanCommand(args, configLoader, commandRunner, workItemProvider, planningAgent, missionRepository, missionEventRecorder);
        }

        if (args[0] == "execute")
        {
            return RunExecuteCommand(args, configLoader, commandRunner, gitRepository, workItemProvider, codingAgent, missionRepository, branchPreparer, missionEventRecorder, runSummaryRepository, runSummaryGenerator);
        }

        if (args[0] == "verify")
        {
            return RunVerifyCommand(args, configLoader, commandRunner, gitRepository, workItemProvider, missionRepository, testRunner, safetyChecker, missionEventRecorder, runSummaryRepository, runSummaryGenerator);
        }

        if (args[0] == "submit")
        {
            return RunSubmitCommand(args, configLoader, commandRunner, gitRepository, workItemProvider, missionRepository, pullRequestProvider, missionEventRecorder, runSummaryRepository, runSummaryGenerator);
        }

        if (args[0] == "run-once")
        {
            return RunRunOnceCommand(args, configLoader, commandRunner, gitRepository, workItemProvider, planningAgent, codingAgent, testRunner, safetyChecker, pullRequestProvider, missionRepository, branchPreparer, dependencyResolver, missionEventRecorder, runSummaryRepository, runSummaryGenerator);
        }

        if (args[0] == "status")
        {
            return RunStatusCommand(args, configLoader, missionRepository);
        }

        if (args[0] == "sync")
        {
            return RunSyncCommand(args, configLoader, commandRunner, workItemProvider, missionRepository, missionEventRecorder);
        }

        if (args[0] == "resume")
        {
            return RunResumeCommand(args, configLoader, commandRunner, gitRepository, workItemProvider, planningAgent, codingAgent, testRunner, safetyChecker, pullRequestProvider, missionRepository, branchPreparer, dependencyResolver, missionEventRecorder, runSummaryRepository, runSummaryGenerator);
        }

        if (args[0] == "cancel")
        {
            return RunCancelCommand(args, configLoader, commandRunner, workItemProvider, missionRepository, missionEventRecorder);
        }

        if (args[0] == "daemon")
        {
            return RunDaemonCommand(args, configLoader, commandRunner, gitRepository, workItemProvider, planningAgent, codingAgent, testRunner, safetyChecker, pullRequestProvider, missionRepository, orchestrator, branchPreparer, dependencyResolver, missionEventRecorder, runSummaryRepository, runSummaryGenerator);
        }

        if (args[0] == "events")
        {
            return RunEventsCommand(args, configLoader, missionEventRecorder);
        }

        if (args[0] == "summarize")
        {
            return RunSummarizeCommand(args, configLoader, commandRunner, gitRepository, workItemProvider, missionRepository, runSummaryRepository, runSummaryGenerator);
        }

        var error = $"""
            Unknown command: {args[0]}

            Run `agent-foreman help` to see available commands.

            """;

        return new CommandResult(1, string.Empty, error);
    }

    private static bool IsHelpArgument(string argument)
    {
        return argument is "help" or "--help" or "-h";
    }

    private static bool IsConfigValidateCommand(IReadOnlyList<string> args)
    {
        return args.Count >= 2 && args[0] == "config" && args[1] == "validate";
    }

    private static CommandResult ValidateConfig(IReadOnlyList<string> args, IAgentForemanConfigLoader configLoader)
    {
        var configPath = GetConfigPath(args);
        var result = configLoader.Load(configPath);

        if (result.IsValid)
        {
            return new CommandResult(0, $"Configuration is valid.{Environment.NewLine}", string.Empty);
        }

        return new CommandResult(1, string.Empty, string.Join(Environment.NewLine, result.Errors) + Environment.NewLine);
    }

    private static string GetConfigPath(IReadOnlyList<string> args)
    {
        for (var index = 0; index < args.Count - 1; index++)
        {
            if (args[index] == "--config")
            {
                return args[index + 1];
            }
        }

        return "agent-foreman.yaml";
    }

    private static CommandResult RunDoctor(
        IReadOnlyList<string> args,
        IAgentForemanConfigLoader configLoader,
        IRepositoryChecker repositoryChecker,
        ICommandAvailabilityChecker commandChecker)
    {
        var configPath = GetConfigPath(args);
        var configResult = configLoader.Load(configPath);
        var output = new List<string>
        {
            "Agent Foreman Doctor",
            string.Empty,
            "Config:",
        };
        var failed = false;

        if (configResult.IsValid)
        {
            output.Add($"  [OK] Config file found: {configPath}");
            output.Add("  [OK] Configuration is valid.");
        }
        else
        {
            output.Add($"  [FAIL] Config file found: {configPath}");
            foreach (var error in configResult.Errors)
            {
                output.Add($"  [FAIL] {error}");
            }

            failed = true;
        }

        output.Add(string.Empty);
        output.Add("Repository:");

        if (configResult.Config is null)
        {
            output.Add("  [FAIL] Repository checks skipped because configuration is invalid.");
        }
        else
        {
            var repoPath = configResult.Config.Project.RepoPath;
            if (repositoryChecker.DirectoryExists(repoPath))
            {
                output.Add($"  [OK] Repo path exists: {repoPath}");
            }
            else
            {
                output.Add($"  [FAIL] Repo path exists: {repoPath}");
                failed = true;
            }

            if (repositoryChecker.IsGitRepository(repoPath))
            {
                output.Add("  [OK] Repo path is a git repository.");
            }
            else
            {
                output.Add("  [FAIL] Repo path is a git repository.");
                failed = true;
            }
        }

        output.Add(string.Empty);
        output.Add("Tools:");

        if (configResult.Config is null)
        {
            output.Add("  [FAIL] Tool checks skipped because configuration is invalid.");
        }
        else
        {
            foreach (var command in GetRequiredCommands(configResult.Config))
            {
                if (commandChecker.IsAvailable(command))
                {
                    output.Add($"  [OK] {command} found.");
                }
                else
                {
                    output.Add($"  [FAIL] {command} not found in PATH.");
                    failed = true;
                }
            }
        }

        output.Add(string.Empty);
        output.Add("Result:");
        output.Add(failed ? "  One or more checks failed." : "  All checks passed.");

        return new CommandResult(failed ? 1 : 0, string.Join(Environment.NewLine, output) + Environment.NewLine, string.Empty);
    }

    private static IReadOnlyList<string> GetRequiredCommands(AgentForemanConfig config)
    {
        return new[] { "git", "gh", config.Planner.Command, config.Executor.Command };
    }

    private static CommandResult RunStateCommand(
        IReadOnlyList<string> args,
        IAgentForemanConfigLoader configLoader,
        IStateStore stateStore)
    {
        if (args.Count < 2)
        {
            return new CommandResult(1, string.Empty, $"Unknown command: state{Environment.NewLine}");
        }

        var configResult = configLoader.Load(GetConfigPath(args));
        if (!configResult.IsValid || configResult.Config is null)
        {
            return new CommandResult(1, string.Empty, string.Join(Environment.NewLine, configResult.Errors) + Environment.NewLine);
        }

        try
        {
            if (args[1] == "init")
            {
                stateStore.Initialize(configResult.Config);
                return new CommandResult(
                    0,
                    $"""
                    State database initialized.
                    Provider: {configResult.Config.Database.Provider}

                    """,
                    string.Empty);
            }

            if (args[1] == "status")
            {
                var status = stateStore.GetStatus(configResult.Config);
                return new CommandResult(
                    0,
                    $"""
                    State database status
                    Provider: {status.Provider}
                    Missions: {status.MissionCount}
                    Provider states: {status.ProviderStateCount}

                    """,
                    string.Empty);
            }
        }
        catch (Exception exception)
        {
            return new CommandResult(1, string.Empty, $"State database error: {exception.Message}{Environment.NewLine}");
        }

        return new CommandResult(1, string.Empty, $"Unknown command: state {args[1]}{Environment.NewLine}");
    }

    private static CommandResult RunExecCommand(
        IReadOnlyList<string> args,
        CoreICommandRunner commandRunner,
        Action<CoreCommandOutputLine>? onExecOutputLine)
    {
        var separatorIndex = -1;
        for (var index = 1; index < args.Count; index++)
        {
            if (args[index] == "--")
            {
                separatorIndex = index;
                break;
            }
        }

        if (separatorIndex < 0 || separatorIndex == args.Count - 1)
        {
            return new CommandResult(1, string.Empty, $"Usage: agent-foreman exec -- <command> [args...]{Environment.NewLine}");
        }

        var executable = args[separatorIndex + 1];
        var commandArgs = args.Skip(separatorIndex + 2).ToArray();
        var request = new CoreCommandRequest(executable, commandArgs);
        var result = commandRunner.RunAsync(request, onExecOutputLine, CancellationToken.None).GetAwaiter().GetResult();

        return new CommandResult(result.ExitCode, $"Exit code: {result.ExitCode}{Environment.NewLine}", string.Empty);
    }

    private static void WriteExecOutputLine(CoreCommandOutputLine line)
    {
        if (line.Stream == CoreCommandOutputStream.Stdout)
        {
            Console.Out.WriteLine(line.Content);
        }
        else
        {
            Console.Error.WriteLine(line.Content);
        }
    }

    private static CommandResult RunGitCommand(
        IReadOnlyList<string> args,
        IAgentForemanConfigLoader configLoader,
        IGitRepository gitRepository)
    {
        if (args.Count < 2)
        {
            return new CommandResult(1, string.Empty, $"Unknown command: git{Environment.NewLine}");
        }

        var configResult = configLoader.Load(GetConfigPath(args));
        if (!configResult.IsValid || configResult.Config is null)
        {
            return new CommandResult(1, string.Empty, string.Join(Environment.NewLine, configResult.Errors) + Environment.NewLine);
        }

        var repoPath = configResult.Config.Project.RepoPath;
        if (string.IsNullOrWhiteSpace(repoPath) || !gitRepository.IsRepositoryAsync(repoPath).GetAwaiter().GetResult())
        {
            return new CommandResult(1, string.Empty, $"Configured repoPath is not a git repository: {repoPath}{Environment.NewLine}");
        }

        if (args[1] == "status")
        {
            var branch = gitRepository.GetCurrentBranchAsync(repoPath).GetAwaiter().GetResult();
            var status = gitRepository.GetStatusAsync(repoPath).GetAwaiter().GetResult();
            var output = new List<string>
            {
                "Git status",
                $"Repo: {repoPath}",
                $"Branch: {branch}",
                "Changed files:",
            };

            output.AddRange(status.ChangedFiles.Count == 0
                ? new[] { "  none" }
                : status.ChangedFiles.Select(file => $"  {file.StatusCode} {file.Path}"));

            return new CommandResult(0, string.Join(Environment.NewLine, output) + Environment.NewLine, string.Empty);
        }

        if (args[1] == "diff")
        {
            var diff = gitRepository.GetDiffAsync(repoPath).GetAwaiter().GetResult();
            return new CommandResult(0, diff, string.Empty);
        }

        return new CommandResult(1, string.Empty, $"Unknown command: git {args[1]}{Environment.NewLine}");
    }

    private static CommandResult RunWorkItemsCommand(
        IReadOnlyList<string> args,
        IAgentForemanConfigLoader configLoader,
        CoreICommandRunner commandRunner,
        IWorkItemDependencyResolver? dependencyResolver)
    {
        if (args.Count < 2)
        {
            return new CommandResult(1, string.Empty, $"Unknown command: work-items{Environment.NewLine}");
        }

        var configResult = configLoader.Load(GetConfigPath(args));
        if (!configResult.IsValid || configResult.Config is null)
        {
            return new CommandResult(1, string.Empty, string.Join(Environment.NewLine, configResult.Errors) + Environment.NewLine);
        }

        if (!string.Equals(configResult.Config.WorkItems.Provider, "github", StringComparison.OrdinalIgnoreCase))
        {
            return new CommandResult(1, string.Empty, $"Unsupported work item provider: {configResult.Config.WorkItems.Provider}{Environment.NewLine}");
        }

        var config = configResult.Config;
        var provider = new GitHubWorkItemProvider(config, commandRunner);
        dependencyResolver ??= new GitHubWorkItemDependencyResolver(config, commandRunner);

        try
        {
            if (args[1] == "ready")
            {
                var items = provider.GetReadyItemsAsync(CancellationToken.None).GetAwaiter().GetResult();
                var lines = new List<string>();
                foreach (var item in items)
                {
                    lines.Add($"#{item.ExternalId} {item.Title}");
                    lines.Add(item.Url);
                    lines.Add($"Labels: {string.Join(", ", item.Labels.Select(label => label.Name))}");
                    var dependencies = item.Dependencies ?? Array.Empty<WorkItemDependency>();
                    if (dependencies.Count > 0)
                    {
                        lines.Add("Dependencies:");
                        foreach (var dependency in dependencies
                                     .Select(dependency => dependencyResolver.ResolveAsync(dependency, CancellationToken.None).GetAwaiter().GetResult()))
                        {
                            lines.Add($"  #{dependency.Reference} {FormatDependencyStatus(dependency.Status)} - {FormatDependencyResolution(dependency.Status)}");
                        }
                    }
                }

                return new CommandResult(0, string.Join(Environment.NewLine, lines) + (lines.Count == 0 ? string.Empty : Environment.NewLine), string.Empty);
            }

            if (args[1] == "view" && args.Count >= 3)
            {
                var item = provider.GetWorkItemAsync(args[2], CancellationToken.None).GetAwaiter().GetResult();
                var output = $"""
                    #{item.ExternalId} {item.Title}
                    Url: {item.Url}
                    Labels: {string.Join(", ", item.Labels.Select(label => label.Name))}

                    {item.Body}
                    """;
                return new CommandResult(0, output + Environment.NewLine, string.Empty);
            }
        }
        catch (Exception exception)
        {
            return new CommandResult(1, string.Empty, $"Work item provider error: {exception.Message}{Environment.NewLine}");
        }

        return new CommandResult(1, string.Empty, $"Unknown command: work-items {args[1]}{Environment.NewLine}");
    }

    private static CommandResult RunLabelsCommand(
        IReadOnlyList<string> args,
        IAgentForemanConfigLoader configLoader,
        CoreICommandRunner commandRunner,
        ILabelManager? labelManager)
    {
        if (args.Count < 2)
        {
            return new CommandResult(1, string.Empty, $"Unknown command: labels{Environment.NewLine}");
        }

        var configResult = configLoader.Load(GetConfigPath(args));
        if (!configResult.IsValid || configResult.Config is null)
        {
            return new CommandResult(1, string.Empty, string.Join(Environment.NewLine, configResult.Errors) + Environment.NewLine);
        }

        var config = configResult.Config;
        if (!string.Equals(config.WorkItems.Provider, "github", StringComparison.OrdinalIgnoreCase))
        {
            return new CommandResult(1, string.Empty, $"Unsupported work item provider: {config.WorkItems.Provider}{Environment.NewLine}");
        }

        labelManager ??= new GitHubLabelManager(commandRunner);

        try
        {
            if (args[1] == "list")
            {
                var labels = labelManager.ListAsync(config.WorkItems.Repo, CancellationToken.None).GetAwaiter().GetResult();
                return new CommandResult(0, string.Join(Environment.NewLine, labels) + (labels.Count == 0 ? string.Empty : Environment.NewLine), string.Empty);
            }

            if (args[1] == "sync")
            {
                var result = labelManager.SyncAsync(config, CancellationToken.None).GetAwaiter().GetResult();
                var lines = result.Results.Select(item => $"{item.Name}: {(item.Created ? "created" : "exists")}");
                return new CommandResult(0, string.Join(Environment.NewLine, lines) + Environment.NewLine, string.Empty);
            }
        }
        catch (Exception exception)
        {
            return new CommandResult(1, string.Empty, $"Label manager error: {exception.Message}{Environment.NewLine}");
        }

        return new CommandResult(1, string.Empty, $"Unknown command: labels {args[1]}{Environment.NewLine}");
    }

    private static CommandResult RunPlanCommand(
        IReadOnlyList<string> args,
        IAgentForemanConfigLoader configLoader,
        CoreICommandRunner commandRunner,
        IWorkItemProvider? workItemProvider,
        IPlanningAgent? planningAgent,
        IMissionRepository? missionRepository,
        IMissionEventRecorder? missionEventRecorder)
    {
        if (args.Count < 2)
        {
            return new CommandResult(1, string.Empty, $"Usage: agent-foreman plan <workItemId>{Environment.NewLine}");
        }

        var configResult = configLoader.Load(GetConfigPath(args));
        if (!configResult.IsValid || configResult.Config is null)
        {
            return new CommandResult(1, string.Empty, string.Join(Environment.NewLine, configResult.Errors) + Environment.NewLine);
        }

        var config = configResult.Config;
        if (!string.Equals(config.WorkItems.Provider, "github", StringComparison.OrdinalIgnoreCase))
        {
            return new CommandResult(1, string.Empty, $"Unsupported work item provider: {config.WorkItems.Provider}{Environment.NewLine}");
        }

        if (!string.Equals(config.Planner.Provider, "claude-cli", StringComparison.OrdinalIgnoreCase))
        {
            return new CommandResult(1, string.Empty, $"Unsupported planner provider: {config.Planner.Provider}{Environment.NewLine}");
        }

        workItemProvider ??= new GitHubWorkItemProvider(config, commandRunner);
        planningAgent ??= new ClaudeCliPlanningAgent(commandRunner);
        missionRepository ??= new PostgresMissionRepository(config.Database.ConnectionString);
        missionEventRecorder ??= new PostgresMissionEventRecorder(config.Database.ConnectionString);

        WorkItem item;
        try
        {
            item = workItemProvider.GetWorkItemAsync(args[1], CancellationToken.None).GetAwaiter().GetResult();
        }
        catch (Exception exception)
        {
            return new CommandResult(1, string.Empty, $"Work item not found: {args[1]}. {exception.Message}{Environment.NewLine}");
        }

        var now = DateTimeOffset.UtcNow;
        var missionId = CreateMissionId(item);
        var existingMission = missionRepository.GetById(missionId);
        var planningMission = (existingMission ?? new Mission(
                missionId,
                item.ExternalId,
                item.Source.ToString(),
                item.Title,
                MissionStatus.New,
                Branch: null,
                PlanPath: null,
                PullRequestUrl: null,
                RetryAfter: null,
                LastError: null,
                CreatedAt: now,
                UpdatedAt: now))
            with
            {
                Title = item.Title,
                Status = MissionStatus.Planning,
                LastError = null,
                UpdatedAt = now,
            };
        missionRepository.Save(planningMission);
        RecordMissionEvent(
            missionEventRecorder,
            CreateMissionEvent(planningMission.Id, item.ExternalId, MissionEventType.PlanningStarted, MissionEventLevel.Info, "Creating technical plan."));

        var outputDirectory = Path.Combine(config.Project.RepoPath, ".agent", "runs", $"issue-{SanitizePathSegment(item.ExternalId)}");
        var request = new PlanningRequest(
            item.ExternalId,
            item.Title,
            item.Body,
            item.Labels,
            item.Repository,
            config.Project.RepoPath,
            outputDirectory,
            ReadAgentsFile(config.Project.RepoPath),
            config);

        PlanningResult planResult;
        try
        {
            planResult = planningAgent.CreatePlanAsync(request, CancellationToken.None).GetAwaiter().GetResult();
        }
        catch (Exception exception)
        {
            var failedMission = planningMission with
            {
                Status = MissionStatus.Failed,
                LastError = exception.Message,
                UpdatedAt = DateTimeOffset.UtcNow,
            };
            missionRepository.Save(failedMission);
            RecordMissionEvent(
                missionEventRecorder,
                CreateMissionEvent(failedMission.Id, item.ExternalId, MissionEventType.PlanningFailed, MissionEventLevel.Error, $"Planning failed: {exception.Message}"));
            return new CommandResult(1, string.Empty, $"Planning failed: {exception.Message}{Environment.NewLine}");
        }

        if (planResult.Success)
        {
            var completedMission = planningMission with
            {
                Status = MissionStatus.PlanReady,
                PlanPath = planResult.PlanPath,
                LastError = null,
                UpdatedAt = DateTimeOffset.UtcNow,
            };
            missionRepository.Save(completedMission);
            RecordMissionEvent(
                missionEventRecorder,
                CreateMissionEvent(
                    completedMission.Id,
                    item.ExternalId,
                    MissionEventType.PlanningCompleted,
                    MissionEventLevel.Info,
                    "Plan created.",
                    $$"""{"planPath":"{{EscapeJson(planResult.PlanPath)}}"}"""));
            return new CommandResult(0, $"Plan created: {planResult.PlanPath}{Environment.NewLine}", string.Empty);
        }

        if (IsQuotaFailure(planResult, config))
        {
            var retryAfter = DateTimeOffset.UtcNow.AddHours(config.Quota.RetryAfterHours ?? 1);
            var reason = "Claude quota or rate limit detected while creating the plan.";
            var pausedMission = planningMission with
            {
                Status = MissionStatus.PausedQuota,
                PlanPath = planResult.PlanPath,
                RetryAfter = retryAfter,
                LastError = planResult.ErrorMessage,
                UpdatedAt = DateTimeOffset.UtcNow,
            };
            missionRepository.Save(pausedMission);
            RecordMissionEvent(
                missionEventRecorder,
                CreateMissionEvent(
                    pausedMission.Id,
                    item.ExternalId,
                    MissionEventType.MissionPausedQuota,
                    MissionEventLevel.Warning,
                    reason,
                    $$"""{"retryAfter":"{{retryAfter:O}}"}"""));
            try { workItemProvider.MarkAsPausedAsync(item, reason, retryAfter, CancellationToken.None).GetAwaiter().GetResult(); } catch { }
            return new CommandResult(1, string.Empty, $"{reason} Retry after: {retryAfter:O}{Environment.NewLine}");
        }

        var failedPlanningMission = planningMission with
        {
            Status = MissionStatus.Failed,
            PlanPath = planResult.PlanPath,
            LastError = planResult.ErrorMessage,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
        missionRepository.Save(failedPlanningMission);
        RecordMissionEvent(
            missionEventRecorder,
            CreateMissionEvent(
                failedPlanningMission.Id,
                item.ExternalId,
                MissionEventType.PlanningFailed,
                MissionEventLevel.Error,
                $"Planning failed: {planResult.ErrorMessage ?? "Claude planner failed."}"));
        return new CommandResult(1, string.Empty, $"Planning failed: {planResult.ErrorMessage ?? "Claude planner failed."}{Environment.NewLine}");
    }

    private static string CreateMissionId(WorkItem item)
    {
        return $"{item.Source.ToString().ToLowerInvariant()}-{item.ExternalId}";
    }

    private static string SanitizePathSegment(string value)
    {
        foreach (var invalid in Path.GetInvalidFileNameChars().Concat(new[] { '/', '\\' }))
        {
            value = value.Replace(invalid, '-');
        }

        return value;
    }

    private static string? ReadAgentsFile(string repoPath)
    {
        var path = Path.Combine(repoPath, "AGENTS.md");
        return File.Exists(path) ? File.ReadAllText(path) : null;
    }

    private static bool IsQuotaFailure(PlanningResult result, AgentForemanConfig config)
    {
        var output = $"{result.Stdout}{Environment.NewLine}{result.Stderr}{Environment.NewLine}{result.ErrorMessage}";
        return config.Quota.QuotaPatterns.Any(pattern =>
            !string.IsNullOrWhiteSpace(pattern)
            && output.Contains(pattern, StringComparison.OrdinalIgnoreCase));
    }

    private static CommandResult RunExecuteCommand(
        IReadOnlyList<string> args,
        IAgentForemanConfigLoader configLoader,
        CoreICommandRunner commandRunner,
        IGitRepository gitRepository,
        IWorkItemProvider? workItemProvider,
        ICodingAgent? codingAgent,
        IMissionRepository? missionRepository,
        IMissionBranchPreparer? branchPreparer = null,
        IMissionEventRecorder? missionEventRecorder = null,
        IRunSummaryRepository? runSummaryRepository = null,
        IRunSummaryGenerator? runSummaryGenerator = null)
    {
        if (args.Count < 2)
        {
            return new CommandResult(1, string.Empty, $"Usage: agent-foreman execute <workItemId>{Environment.NewLine}");
        }

        var configResult = configLoader.Load(GetConfigPath(args));
        if (!configResult.IsValid || configResult.Config is null)
        {
            return new CommandResult(1, string.Empty, string.Join(Environment.NewLine, configResult.Errors) + Environment.NewLine);
        }

        var config = configResult.Config;
        if (!string.Equals(config.WorkItems.Provider, "github", StringComparison.OrdinalIgnoreCase))
        {
            return new CommandResult(1, string.Empty, $"Unsupported work item provider: {config.WorkItems.Provider}{Environment.NewLine}");
        }

        if (!CodingAgentFactory.IsSupported(config.Executor.Provider))
        {
            return new CommandResult(1, string.Empty, $"Unsupported executor provider: {config.Executor.Provider}{Environment.NewLine}");
        }

        workItemProvider ??= new GitHubWorkItemProvider(config, commandRunner);
        codingAgent ??= CodingAgentFactory.Create(config, commandRunner);
        missionRepository ??= new PostgresMissionRepository(config.Database.ConnectionString);
        missionEventRecorder ??= new PostgresMissionEventRecorder(config.Database.ConnectionString);
        var runSummaryService = CreateRunSummaryService(config, commandRunner, gitRepository, runSummaryRepository, runSummaryGenerator);

        WorkItem item;
        try
        {
            item = workItemProvider.GetWorkItemAsync(args[1], CancellationToken.None).GetAwaiter().GetResult();
        }
        catch (Exception exception)
        {
            return new CommandResult(1, string.Empty, $"Work item not found: {args[1]}. {exception.Message}{Environment.NewLine}");
        }

        var outputDirectory = Path.Combine(config.Project.RepoPath, ".agent", "runs", $"issue-{SanitizePathSegment(item.ExternalId)}");
        var planPath = Path.Combine(outputDirectory, "plan.md");
        if (!File.Exists(planPath))
        {
            return new CommandResult(1, string.Empty, $"Plan not found. Run `agent-foreman plan {item.ExternalId}` first.{Environment.NewLine}");
        }

        var now = DateTimeOffset.UtcNow;
        var missionId = CreateMissionId(item);
        var existingMission = missionRepository.GetById(missionId);
        var codingMission = (existingMission ?? new Mission(
                missionId,
                item.ExternalId,
                item.Source.ToString(),
                item.Title,
                MissionStatus.New,
                Branch: null,
                PlanPath: planPath,
                PullRequestUrl: null,
                RetryAfter: null,
                LastError: null,
                CreatedAt: now,
                UpdatedAt: now))
            with
            {
                Title = item.Title,
                Status = MissionStatus.Coding,
                PlanPath = planPath,
                LastError = null,
                UpdatedAt = now,
            };
        missionRepository.Save(codingMission);
        RecordMissionEvent(
            missionEventRecorder,
            CreateMissionEvent(codingMission.Id, item.ExternalId, MissionEventType.ExecutionStarted, MissionEventLevel.Info, "Running Codex."));

        branchPreparer ??= new MissionBranchPreparer(gitRepository);
        var agentBranch = $"agent/issue-{item.ExternalId}";
        var branchPrepResult = branchPreparer.PrepareAsync(
            new BranchPreparationRequest(config.Project.RepoPath, config.Project.DefaultBranch, agentBranch),
            CancellationToken.None).GetAwaiter().GetResult();
        if (!branchPrepResult.Success)
        {
            var failedMission = codingMission with
            {
                Status = MissionStatus.Failed,
                LastError = branchPrepResult.ErrorMessage,
                UpdatedAt = DateTimeOffset.UtcNow,
            };
            missionRepository.Save(failedMission);
            RecordMissionEvent(
                missionEventRecorder,
                CreateMissionEvent(failedMission.Id, item.ExternalId, MissionEventType.ExecutionFailed, MissionEventLevel.Error, $"Branch preparation failed: {branchPrepResult.ErrorMessage}"));
            GenerateMissionSummaries(runSummaryService, failedMission, item, config.Project.RepoPath, outputDirectory);
            return new CommandResult(1, string.Empty, $"Branch preparation failed: {branchPrepResult.ErrorMessage}{Environment.NewLine}");
        }
        RecordMissionEvent(
            missionEventRecorder,
            CreateMissionEvent(codingMission.Id, item.ExternalId, MissionEventType.BranchPrepared, MissionEventLevel.Info, $"Prepared branch {agentBranch}."));

        var planContent = File.ReadAllText(planPath);
        var request = new CodingRequest(
            item.ExternalId,
            item.Title,
            item.Body,
            item.Labels,
            item.Repository,
            config.Project.RepoPath,
            planPath,
            planContent,
            outputDirectory,
            ReadAgentsFile(config.Project.RepoPath),
            PreviousLogs: null,
            CurrentDiff: null,
            config);

        CodingResult codingResult;
        try
        {
            codingResult = codingAgent.ExecuteAsync(request, CancellationToken.None).GetAwaiter().GetResult();
        }
        catch (Exception exception)
        {
            var failedMission = codingMission with
            {
                Status = MissionStatus.Failed,
                LastError = exception.Message,
                UpdatedAt = DateTimeOffset.UtcNow,
            };
            missionRepository.Save(failedMission);
            RecordMissionEvent(
                missionEventRecorder,
                CreateMissionEvent(failedMission.Id, item.ExternalId, MissionEventType.ExecutionFailed, MissionEventLevel.Error, $"Execution failed: {exception.Message}"));
            GenerateMissionSummaries(runSummaryService, failedMission, item, config.Project.RepoPath, outputDirectory);
            return new CommandResult(1, string.Empty, $"Execution failed: {exception.Message}{Environment.NewLine}");
        }

        if (codingResult.Success)
        {
            var completedMission = codingMission with
            {
                Status = MissionStatus.CodingCompleted,
                LastError = null,
                UpdatedAt = DateTimeOffset.UtcNow,
            };
            missionRepository.Save(completedMission);
            RecordMissionEvent(
                missionEventRecorder,
                CreateMissionEvent(
                    completedMission.Id,
                    item.ExternalId,
                    MissionEventType.ExecutionCompleted,
                    MissionEventLevel.Info,
                    "Execution completed.",
                    $$"""{"logPath":"{{EscapeJson(codingResult.LogPath)}}"}"""));
            var output = $"""
                Execution completed.
                Log path: {codingResult.LogPath}

                """;
            return new CommandResult(0, output, string.Empty);
        }

        if (codingResult.QuotaDetected || IsQuotaFailure(codingResult, config))
        {
            var retryAfter = DateTimeOffset.UtcNow.AddHours(config.Quota.RetryAfterHours ?? 1);
            var reason = "Codex quota or rate limit detected while executing the plan.";
            var pausedMission = codingMission with
            {
                Status = MissionStatus.PausedQuota,
                RetryAfter = retryAfter,
                LastError = codingResult.ErrorMessage,
                UpdatedAt = DateTimeOffset.UtcNow,
            };
            missionRepository.Save(pausedMission);
            RecordMissionEvent(
                missionEventRecorder,
                CreateMissionEvent(
                    pausedMission.Id,
                    item.ExternalId,
                    MissionEventType.MissionPausedQuota,
                    MissionEventLevel.Warning,
                    reason,
                    $$"""{"retryAfter":"{{retryAfter:O}}"}"""));
            try { workItemProvider.AddCommentAsync(item, $"{reason} Retry after: {retryAfter:O}", CancellationToken.None).GetAwaiter().GetResult(); } catch { }
            GenerateMissionSummaries(runSummaryService, pausedMission, item, config.Project.RepoPath, outputDirectory);
            return new CommandResult(1, string.Empty, $"{reason} Retry after: {retryAfter:O}{Environment.NewLine}");
        }

        var failedCodingMission = codingMission with
        {
            Status = MissionStatus.Failed,
            LastError = codingResult.ErrorMessage,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
        missionRepository.Save(failedCodingMission);
        RecordMissionEvent(
            missionEventRecorder,
            CreateMissionEvent(
                failedCodingMission.Id,
                item.ExternalId,
                MissionEventType.ExecutionFailed,
                MissionEventLevel.Error,
                $"Execution failed: {codingResult.ErrorMessage ?? "Codex executor failed."}"));
        GenerateMissionSummaries(runSummaryService, failedCodingMission, item, config.Project.RepoPath, outputDirectory);
        return new CommandResult(1, string.Empty, $"Execution failed: {codingResult.ErrorMessage ?? "Codex executor failed."}{Environment.NewLine}");
    }

    private static bool IsQuotaFailure(CodingResult result, AgentForemanConfig config)
    {
        var output = $"{result.Stdout}{Environment.NewLine}{result.Stderr}{Environment.NewLine}{result.ErrorMessage}";
        return config.Quota.QuotaPatterns.Any(pattern =>
            !string.IsNullOrWhiteSpace(pattern)
            && output.Contains(pattern, StringComparison.OrdinalIgnoreCase));
    }

    private static MissionEvent CreateMissionEvent(
        string missionId,
        string? externalWorkItemId,
        MissionEventType eventType,
        MissionEventLevel level,
        string message,
        string? metadataJson = null)
    {
        return new MissionEvent(
            Guid.NewGuid().ToString("N"),
            missionId,
            externalWorkItemId,
            RunId: null,
            eventType,
            level,
            message,
            metadataJson,
            DateTimeOffset.UtcNow);
    }

    private static void RecordMissionEvent(IMissionEventRecorder recorder, MissionEvent missionEvent)
    {
        try
        {
            recorder.AppendMissionEventAsync(missionEvent, CancellationToken.None).GetAwaiter().GetResult();
        }
        catch
        {
        }
    }

    private static string EscapeJson(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal);
    }

    private static CommandResult RunSubmitCommand(
        IReadOnlyList<string> args,
        IAgentForemanConfigLoader configLoader,
        CoreICommandRunner commandRunner,
        IGitRepository gitRepository,
        IWorkItemProvider? workItemProvider,
        IMissionRepository? missionRepository,
        IPullRequestProvider? pullRequestProvider,
        IMissionEventRecorder? missionEventRecorder,
        IRunSummaryRepository? runSummaryRepository = null,
        IRunSummaryGenerator? runSummaryGenerator = null)
    {
        if (args.Count < 2)
        {
            return new CommandResult(1, string.Empty, $"Usage: agent-foreman submit <workItemId>{Environment.NewLine}");
        }

        var configResult = configLoader.Load(GetConfigPath(args));
        if (!configResult.IsValid || configResult.Config is null)
        {
            return new CommandResult(1, string.Empty, string.Join(Environment.NewLine, configResult.Errors) + Environment.NewLine);
        }

        var config = configResult.Config;
        if (!string.Equals(config.WorkItems.Provider, "github", StringComparison.OrdinalIgnoreCase))
        {
            return new CommandResult(1, string.Empty, $"Unsupported work item provider: {config.WorkItems.Provider}{Environment.NewLine}");
        }

        workItemProvider ??= new GitHubWorkItemProvider(config, commandRunner);
        missionRepository ??= new PostgresMissionRepository(config.Database.ConnectionString);
        pullRequestProvider ??= new GitHubPullRequestProvider(commandRunner);
        missionEventRecorder ??= new PostgresMissionEventRecorder(config.Database.ConnectionString);
        var runSummaryService = CreateRunSummaryService(config, commandRunner, gitRepository, runSummaryRepository, runSummaryGenerator);

        WorkItem item;
        try
        {
            item = workItemProvider.GetWorkItemAsync(args[1], CancellationToken.None).GetAwaiter().GetResult();
        }
        catch (Exception exception)
        {
            return new CommandResult(1, string.Empty, $"Work item not found: {args[1]}. {exception.Message}{Environment.NewLine}");
        }

        var missionId = CreateMissionId(item);
        var existingMission = missionRepository.GetById(missionId);
        if (existingMission?.Status != MissionStatus.TestsPassed)
        {
            return new CommandResult(1, string.Empty,
                $"Work item must pass verification before submit. Run `agent-foreman verify {item.ExternalId}` first.{Environment.NewLine}");
        }

        var status = gitRepository.GetStatusAsync(config.Project.RepoPath, CancellationToken.None).GetAwaiter().GetResult();
        if (status.ChangedFiles.Count == 0)
        {
            return new CommandResult(1, string.Empty, $"No changes to submit.{Environment.NewLine}");
        }

        var currentBranch = gitRepository.GetCurrentBranchAsync(config.Project.RepoPath).GetAwaiter().GetResult();
        var agentBranch = $"agent/issue-{item.ExternalId}";

        if (currentBranch != agentBranch)
        {
            return new CommandResult(1, string.Empty,
                $"Expected branch {agentBranch} but currently on {currentBranch}.{Environment.NewLine}" +
                $"Run `agent-foreman execute {item.ExternalId}` to prepare the branch.{Environment.NewLine}");
        }

        var now = DateTimeOffset.UtcNow;
        var submittingMission = existingMission with
        {
            Branch = agentBranch,
            UpdatedAt = now,
        };
        missionRepository.Save(submittingMission);
        RecordMissionEvent(
            missionEventRecorder,
            CreateMissionEvent(submittingMission.Id, item.ExternalId, MissionEventType.SubmitStarted, MissionEventLevel.Info, "Creating pull request."));
        string? summaryDiff = null;
        try
        {
            summaryDiff = gitRepository.GetDiffAsync(config.Project.RepoPath, CancellationToken.None).GetAwaiter().GetResult();
        }
        catch
        {
        }

        gitRepository.AddAllAsync(config.Project.RepoPath).GetAwaiter().GetResult();

        var commitMessage = $"Implement issue #{item.ExternalId}: {item.Title}";
        var commitResult = gitRepository.CommitAsync(config.Project.RepoPath, commitMessage).GetAwaiter().GetResult();
        if (!commitResult.Created)
        {
            return new CommandResult(1, string.Empty, $"Nothing to commit.{Environment.NewLine}");
        }

        try
        {
            gitRepository.PushAsync(config.Project.RepoPath, "origin", agentBranch).GetAwaiter().GetResult();
        }
        catch (Exception exception)
        {
            var failedMission = submittingMission with
            {
                Status = MissionStatus.Failed,
                LastError = exception.Message,
                UpdatedAt = DateTimeOffset.UtcNow,
            };
            missionRepository.Save(failedMission);
            RecordMissionEvent(
                missionEventRecorder,
                CreateMissionEvent(failedMission.Id, item.ExternalId, MissionEventType.SubmitFailed, MissionEventLevel.Error, $"Push failed: {exception.Message}"));
            return new CommandResult(1, string.Empty, $"Push failed: {exception.Message}{Environment.NewLine}");
        }

        var outputDirectory = Path.Combine(config.Project.RepoPath, ".agent", "runs", $"issue-{SanitizePathSegment(item.ExternalId)}");
        var prRequest = new PullRequestRequest(
            item.ExternalId,
            item.Title,
            item.Body,
            item.Repository,
            config.Project.RepoPath,
            agentBranch,
            config.Project.DefaultBranch,
            commitMessage,
            $"Implement issue #{item.ExternalId}: {item.Title}",
            BuildPrBody(item, outputDirectory));

        PullRequestResult prResult;
        try
        {
            prResult = pullRequestProvider.CreateAsync(prRequest, CancellationToken.None).GetAwaiter().GetResult();
        }
        catch (Exception exception)
        {
            var failedMission = submittingMission with
            {
                Status = MissionStatus.Failed,
                LastError = exception.Message,
                UpdatedAt = DateTimeOffset.UtcNow,
            };
            missionRepository.Save(failedMission);
            RecordMissionEvent(
                missionEventRecorder,
                CreateMissionEvent(failedMission.Id, item.ExternalId, MissionEventType.SubmitFailed, MissionEventLevel.Error, $"PR creation failed: {exception.Message}"));
            return new CommandResult(1, string.Empty, $"PR creation failed: {exception.Message}{Environment.NewLine}");
        }

        if (!prResult.Success)
        {
            var failedMission = submittingMission with
            {
                Status = MissionStatus.Failed,
                LastError = prResult.ErrorMessage,
                UpdatedAt = DateTimeOffset.UtcNow,
            };
            missionRepository.Save(failedMission);
            RecordMissionEvent(
                missionEventRecorder,
                CreateMissionEvent(failedMission.Id, item.ExternalId, MissionEventType.SubmitFailed, MissionEventLevel.Error, $"PR creation failed: {prResult.ErrorMessage ?? "gh pr create failed."}"));
            return new CommandResult(1, string.Empty,
                $"PR creation failed: {prResult.ErrorMessage ?? "gh pr create failed."}{Environment.NewLine}");
        }

        var completedMission = submittingMission with
        {
            Status = MissionStatus.PullRequestCreated,
            PullRequestUrl = prResult.PullRequestUrl,
            LastError = null,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
        missionRepository.Save(completedMission);
        RecordMissionEvent(
            missionEventRecorder,
            CreateMissionEvent(
                completedMission.Id,
                item.ExternalId,
                MissionEventType.PullRequestCreated,
                MissionEventLevel.Info,
                "Pull request created.",
                $$"""{"pullRequestUrl":"{{EscapeJson(prResult.PullRequestUrl)}}"}"""));
        var summaryPaths = GenerateMissionSummaries(runSummaryService, completedMission, item, config.Project.RepoPath, outputDirectory, summaryDiff);

        var reviewWarning = string.Empty;
        try
        {
            workItemProvider.MarkAsReviewAsync(item, prResult.PullRequestUrl!, CancellationToken.None).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            reviewWarning = $"Warning: could not update work item labels: {ex.Message}{Environment.NewLine}";
        }

        var output = new StringBuilder();
        output.AppendLine($"Pull request created: {prResult.PullRequestUrl}");
        foreach (var path in summaryPaths)
        {
            output.AppendLine($"Summary: {path}");
        }
        if (!string.IsNullOrWhiteSpace(reviewWarning))
        {
            output.Append(reviewWarning);
        }

        return new CommandResult(0, output.ToString(), string.Empty);
    }

    private static CommandResult RunSummarizeCommand(
        IReadOnlyList<string> args,
        IAgentForemanConfigLoader configLoader,
        CoreICommandRunner commandRunner,
        IGitRepository gitRepository,
        IWorkItemProvider? workItemProvider,
        IMissionRepository? missionRepository,
        IRunSummaryRepository? runSummaryRepository,
        IRunSummaryGenerator? runSummaryGenerator)
    {
        if (args.Count < 2)
        {
            return new CommandResult(1, string.Empty, $"Usage: agent-foreman summarize <missionId>{Environment.NewLine}");
        }

        var configResult = configLoader.Load(GetConfigPath(args));
        if (!configResult.IsValid || configResult.Config is null)
        {
            return new CommandResult(1, string.Empty, string.Join(Environment.NewLine, configResult.Errors) + Environment.NewLine);
        }

        var config = configResult.Config;
        missionRepository ??= new PostgresMissionRepository(config.Database.ConnectionString);
        var missionIdArg = args[1];
        var mission = missionRepository.GetById(missionIdArg);
        if (mission is null && !missionIdArg.Contains('-', StringComparison.Ordinal))
        {
            mission = FindMissionByExternalId(missionRepository, missionIdArg);
        }

        if (mission is null)
        {
            return new CommandResult(1, string.Empty, $"Mission not found: {missionIdArg}{Environment.NewLine}");
        }

        var issueTitle = mission.Title;
        var issueBody = string.Empty;
        var externalId = mission.ExternalWorkItemId;

        if (!string.IsNullOrWhiteSpace(externalId)
            && string.Equals(config.WorkItems.Provider, "github", StringComparison.OrdinalIgnoreCase))
        {
            workItemProvider ??= new GitHubWorkItemProvider(config, commandRunner);

            try
            {
                var item = workItemProvider.GetWorkItemAsync(externalId, CancellationToken.None).GetAwaiter().GetResult();
                issueTitle = item.Title;
                issueBody = item.Body;
            }
            catch
            {
            }
        }

        var outputKey = mission.ExternalWorkItemId ?? mission.Id;
        var outputDirectory = Path.Combine(config.Project.RepoPath, ".agent", "runs", $"issue-{SanitizePathSegment(outputKey)}");
        var runSummaryService = CreateRunSummaryService(config, commandRunner, gitRepository, runSummaryRepository, runSummaryGenerator);
        var summaries = runSummaryService.GenerateAndSaveAsync(
            new RunSummaryGenerationRequest(
                mission,
                config.Project.RepoPath,
                outputDirectory,
                issueTitle,
                issueBody,
                mission.Status,
                GetSummaryTypesForMission(mission.Status),
                mission.PullRequestUrl),
            CancellationToken.None).GetAwaiter().GetResult();

        var output = new StringBuilder();
        foreach (var summary in summaries)
        {
            output.AppendLine($"{summary.SummaryType}: {summary.Path}");
        }

        return new CommandResult(0, output.ToString(), string.Empty);
    }

    private static CommandResult RunResumeCommand(
        IReadOnlyList<string> args,
        IAgentForemanConfigLoader configLoader,
        CoreICommandRunner commandRunner,
        IGitRepository gitRepository,
        IWorkItemProvider? workItemProvider,
        IPlanningAgent? planningAgent,
        ICodingAgent? codingAgent,
        ITestRunner? testRunner,
        ISafetyChecker? safetyChecker,
        IPullRequestProvider? pullRequestProvider,
        IMissionRepository? missionRepository,
        IMissionBranchPreparer? branchPreparer = null,
        IWorkItemDependencyResolver? dependencyResolver = null,
        IMissionEventRecorder? missionEventRecorder = null,
        IRunSummaryRepository? runSummaryRepository = null,
        IRunSummaryGenerator? runSummaryGenerator = null)
    {
        if (args.Count < 2)
        {
            return new CommandResult(1, string.Empty, $"Usage: agent-foreman resume <missionId>{Environment.NewLine}");
        }

        var missionIdArg = args[1];
        var force = args.Contains("--force");

        var configResult = configLoader.Load(GetConfigPath(args));
        if (!configResult.IsValid || configResult.Config is null)
        {
            return new CommandResult(1, string.Empty, string.Join(Environment.NewLine, configResult.Errors) + Environment.NewLine);
        }

        var config = configResult.Config;
        if (!string.Equals(config.WorkItems.Provider, "github", StringComparison.OrdinalIgnoreCase))
        {
            return new CommandResult(1, string.Empty, $"Unsupported work item provider: {config.WorkItems.Provider}{Environment.NewLine}");
        }

        workItemProvider ??= new GitHubWorkItemProvider(config, commandRunner);
        planningAgent ??= new ClaudeCliPlanningAgent(commandRunner);
        codingAgent ??= CodingAgentFactory.Create(config, commandRunner);
        testRunner ??= new ConfiguredCommandTestRunner(commandRunner);
        safetyChecker ??= new GitSafetyChecker(gitRepository);
        pullRequestProvider ??= new GitHubPullRequestProvider(commandRunner);
        missionRepository ??= new PostgresMissionRepository(config.Database.ConnectionString);
        dependencyResolver ??= new GitHubWorkItemDependencyResolver(config, commandRunner);
        missionEventRecorder ??= new PostgresMissionEventRecorder(config.Database.ConnectionString);

        // Resolve external id from the input
        string externalId;
        Mission? mission = null;

        if (int.TryParse(missionIdArg, out _))
        {
            externalId = missionIdArg;
        }
        else if (missionIdArg.StartsWith("issue-", StringComparison.OrdinalIgnoreCase))
        {
            externalId = missionIdArg["issue-".Length..];
        }
        else
        {
            mission = missionRepository.GetById(missionIdArg);
            if (mission is null)
            {
                return new CommandResult(1, string.Empty, $"Mission not found: {missionIdArg}{Environment.NewLine}");
            }

            externalId = mission.ExternalWorkItemId ?? missionIdArg;
        }

        WorkItem item;
        try
        {
            item = workItemProvider.GetWorkItemAsync(externalId, CancellationToken.None).GetAwaiter().GetResult();
        }
        catch (Exception exception)
        {
            return new CommandResult(1, string.Empty, $"Work item not found: {externalId}. {exception.Message}{Environment.NewLine}");
        }

        if (mission is null)
        {
            var missionId = CreateMissionId(item);
            mission = missionRepository.GetById(missionId);
            if (mission is null)
            {
                return new CommandResult(1, string.Empty, $"Mission not found for work item: {externalId}{Environment.NewLine}");
            }
        }

        if (mission.Status == MissionStatus.PullRequestCreated)
        {
            return new CommandResult(0,
                $"Mission already completed.{Environment.NewLine}PR: {mission.PullRequestUrl}{Environment.NewLine}",
                string.Empty);
        }

        if (mission.Status == MissionStatus.PausedQuota && !force && mission.RetryAfter > DateTimeOffset.UtcNow)
        {
            return new CommandResult(1, string.Empty,
                $"Mission is paused until {mission.RetryAfter:O}.{Environment.NewLine}");
        }

        if (mission.Status == MissionStatus.Failed && !force)
        {
            return new CommandResult(1, string.Empty,
                $"Mission is failed. Use --force to retry.{Environment.NewLine}");
        }

        var runSummaryService = CreateRunSummaryService(config, commandRunner, gitRepository, runSummaryRepository, runSummaryGenerator);
        branchPreparer ??= new MissionBranchPreparer(gitRepository);
        var orchestrator = new MissionOrchestrator(
            workItemProvider, planningAgent, codingAgent, testRunner, safetyChecker,
            pullRequestProvider, gitRepository, missionRepository, branchPreparer, dependencyResolver, missionEventRecorder, runSummaryService);

        var output = new StringBuilder();
        ResumeResult result;
        try
        {
            result = orchestrator.ResumeAsync(
                new ResumeRequest(config, mission, item, force),
                msg => output.AppendLine(msg),
                CancellationToken.None).GetAwaiter().GetResult();
        }
        catch (Exception exception)
        {
            return new CommandResult(1, output.ToString(), $"Resume failed: {exception.Message}{Environment.NewLine}");
        }

        if (result.Success)
        {
            if (result.PullRequestUrl is not null)
                output.AppendLine($"Pull request created: {result.PullRequestUrl}");
            return new CommandResult(0, output.ToString(), string.Empty);
        }

        if (result.QuotaDetected)
        {
            return new CommandResult(1, output.ToString(), $"Quota detected. Retry after: {result.RetryAfter:O}{Environment.NewLine}");
        }

        return new CommandResult(1, output.ToString(), $"{result.ErrorMessage}{Environment.NewLine}");
    }

    private static CommandResult RunStatusCommand(
        IReadOnlyList<string> args,
        IAgentForemanConfigLoader configLoader,
        IMissionRepository? missionRepository)
    {
        var configResult = configLoader.Load(GetConfigPath(args));
        if (!configResult.IsValid || configResult.Config is null)
        {
            return new CommandResult(1, string.Empty, string.Join(Environment.NewLine, configResult.Errors) + Environment.NewLine);
        }

        var config = configResult.Config;
        missionRepository ??= new PostgresMissionRepository(config.Database.ConnectionString);

        var showAll = args.Contains("--all");

        string? statusFilter = null;
        for (var i = 1; i < args.Count - 1; i++)
        {
            if (args[i] == "--status")
            {
                statusFilter = args[i + 1];
                break;
            }
        }

        MissionStatus? filterStatus = null;
        if (statusFilter is not null)
        {
            if (!Enum.TryParse<MissionStatus>(statusFilter, ignoreCase: true, out var parsed))
            {
                var validValues = string.Join(", ", Enum.GetNames<MissionStatus>());
                return new CommandResult(1, string.Empty,
                    $"Invalid status: {statusFilter}. Valid values: {validValues}{Environment.NewLine}");
            }

            filterStatus = parsed;
        }

        var limit = showAll ? int.MaxValue : 20;

        IReadOnlyList<Mission> missions;
        try
        {
            missions = filterStatus.HasValue
                ? missionRepository.GetByStatus(filterStatus.Value, limit)
                : missionRepository.GetRecent(limit);
            missions = OrderMissionsForDisplay(missions).ToList();
        }
        catch (Exception exception)
        {
            return new CommandResult(1, string.Empty, $"Database error: {exception.Message}{Environment.NewLine}");
        }

        var output = new StringBuilder();
        output.AppendLine("Agent Foreman Status");
        output.AppendLine();

        if (missions.Count == 0)
        {
            output.AppendLine("No missions found.");
            return new CommandResult(0, output.ToString(), string.Empty);
        }

        output.AppendLine("Missions:");
        foreach (var mission in missions)
        {
            output.AppendLine($"  {mission.Id}  #{mission.ExternalWorkItemId}  {mission.Title}");
            output.AppendLine($"    Status: {mission.Status}");
            if (mission.Branch is not null)
                output.AppendLine($"    Branch: {mission.Branch}");
            if (mission.PullRequestUrl is not null)
                output.AppendLine($"    PR: {mission.PullRequestUrl}");
            if (mission.RetryAfter.HasValue)
                output.AppendLine($"    Retry after: {mission.RetryAfter.Value:O}");
            if (mission.LastError is not null)
                output.AppendLine($"    Last error: {mission.LastError}");
            output.AppendLine($"    Updated: {mission.UpdatedAt:O}");
        }

        return new CommandResult(0, output.ToString(), string.Empty);
    }

    private static IEnumerable<Mission> OrderMissionsForDisplay(IEnumerable<Mission> missions)
    {
        return missions
            .OrderByDescending(GetMissionSortNumber)
            .ThenByDescending(mission => mission.UpdatedAt)
            .ThenByDescending(mission => mission.Id, StringComparer.OrdinalIgnoreCase);
    }

    private static int GetMissionSortNumber(Mission mission)
    {
        if (int.TryParse(mission.ExternalWorkItemId, out var externalNumber))
        {
            return externalNumber;
        }

        var marker = mission.Id.LastIndexOf("-", StringComparison.Ordinal);
        if (marker >= 0 && int.TryParse(mission.Id[(marker + 1)..], out var idNumber))
        {
            return idNumber;
        }

        return int.MinValue;
    }

    private static CommandResult RunSyncCommand(
        IReadOnlyList<string> args,
        IAgentForemanConfigLoader configLoader,
        CoreICommandRunner commandRunner,
        IWorkItemProvider? workItemProvider,
        IMissionRepository? missionRepository,
        IMissionEventRecorder? missionEventRecorder)
    {
        var configResult = configLoader.Load(GetConfigPath(args));
        if (!configResult.IsValid || configResult.Config is null)
        {
            return new CommandResult(1, string.Empty, string.Join(Environment.NewLine, configResult.Errors) + Environment.NewLine);
        }

        var config = configResult.Config;
        if (!string.Equals(config.WorkItems.Provider, "github", StringComparison.OrdinalIgnoreCase))
        {
            return new CommandResult(1, string.Empty, $"Unsupported work item provider: {config.WorkItems.Provider}{Environment.NewLine}");
        }

        var dryRun = args.Contains("--dry-run");
        workItemProvider ??= new GitHubWorkItemProvider(config, commandRunner);
        missionRepository ??= new PostgresMissionRepository(config.Database.ConnectionString);
        missionEventRecorder ??= new PostgresMissionEventRecorder(config.Database.ConnectionString);

        IReadOnlyList<Mission> missions;
        try
        {
            missions = missionRepository.GetRecent(100);
        }
        catch (Exception exception)
        {
            return new CommandResult(1, string.Empty, $"Database error: {exception.Message}{Environment.NewLine}");
        }

        var output = new StringBuilder();
        output.AppendLine("Synchronizing missions...");
        output.AppendLine();

        foreach (var mission in missions.Where(m => !string.IsNullOrWhiteSpace(m.ExternalWorkItemId)))
        {
            WorkItem item;
            try
            {
                item = workItemProvider.GetWorkItemAsync(mission.ExternalWorkItemId!, CancellationToken.None).GetAwaiter().GetResult();
            }
            catch (Exception exception)
            {
                output.AppendLine(mission.Id);
                output.AppendLine($"  Error: {exception.Message}");
                output.AppendLine();
                continue;
            }

            output.AppendLine(mission.Id);
            output.AppendLine($"  GitHub state: {item.State.ToString().ToLowerInvariant()}");
            output.AppendLine($"  Local status: {mission.Status}");

            if (item.State == WorkItemState.Closed)
            {
                output.AppendLine("  Action: mark mission Completed, remove agent-review");

                if (!dryRun)
                {
                    var completedMission = mission with
                    {
                        Status = MissionStatus.Completed,
                        LastError = null,
                        UpdatedAt = DateTimeOffset.UtcNow,
                    };
                    missionRepository.Save(completedMission);
                    workItemProvider.MarkAsCompletedAsync(item, CancellationToken.None).GetAwaiter().GetResult();
                    RecordMissionEvent(
                        missionEventRecorder,
                        CreateMissionEvent(completedMission.Id, completedMission.ExternalWorkItemId, MissionEventType.MissionCompleted, MissionEventLevel.Info, $"Mission synchronized as completed from closed GitHub issue #{item.ExternalId}."));
                }
            }
            else if (mission.Status == MissionStatus.PullRequestCreated)
            {
                output.AppendLine("  Action: keep waiting for review");
            }
            else
            {
                output.AppendLine("  Action: no changes");
            }

            output.AppendLine();
        }

        output.AppendLine("Sync completed.");
        return new CommandResult(0, output.ToString(), string.Empty);
    }

    private static CommandResult RunEventsCommand(
        IReadOnlyList<string> args,
        IAgentForemanConfigLoader configLoader,
        IMissionEventRecorder? missionEventRecorder)
    {
        if (args.Count < 2)
        {
            return new CommandResult(1, string.Empty, $"Usage: agent-foreman events <missionId> [--limit <number>]{Environment.NewLine}");
        }

        var configResult = configLoader.Load(GetConfigPath(args));
        if (!configResult.IsValid || configResult.Config is null)
        {
            return new CommandResult(1, string.Empty, string.Join(Environment.NewLine, configResult.Errors) + Environment.NewLine);
        }

        var config = configResult.Config;
        missionEventRecorder ??= new PostgresMissionEventRecorder(config.Database.ConnectionString);

        var limit = 50;
        for (var i = 2; i < args.Count - 1; i++)
        {
            if (args[i] == "--limit" && int.TryParse(args[i + 1], out var parsedLimit) && parsedLimit > 0)
            {
                limit = parsedLimit;
                break;
            }
        }

        IReadOnlyList<MissionEvent> events;
        try
        {
            events = missionEventRecorder.GetMissionEventsAsync(args[1], limit, CancellationToken.None).GetAwaiter().GetResult();
        }
        catch (Exception exception)
        {
            return new CommandResult(1, string.Empty, $"Database error: {exception.Message}{Environment.NewLine}");
        }

        var output = new StringBuilder();
        output.AppendLine($"Events for {args[1]}");
        output.AppendLine();

        foreach (var missionEvent in events)
        {
            output.AppendLine($"{missionEvent.CreatedAt:O} [{missionEvent.Level}] {missionEvent.EventType} - {missionEvent.Message}");
        }

        return new CommandResult(0, output.ToString(), string.Empty);
    }

    private static CommandResult RunDaemonCommand(
        IReadOnlyList<string> args,
        IAgentForemanConfigLoader configLoader,
        CoreICommandRunner commandRunner,
        IGitRepository gitRepository,
        IWorkItemProvider? workItemProvider,
        IPlanningAgent? planningAgent,
        ICodingAgent? codingAgent,
        ITestRunner? testRunner,
        ISafetyChecker? safetyChecker,
        IPullRequestProvider? pullRequestProvider,
        IMissionRepository? missionRepository,
        IMissionOrchestrator? orchestrator,
        IMissionBranchPreparer? branchPreparer = null,
        IWorkItemDependencyResolver? dependencyResolver = null,
        IMissionEventRecorder? missionEventRecorder = null,
        IRunSummaryRepository? runSummaryRepository = null,
        IRunSummaryGenerator? runSummaryGenerator = null)
    {
        var runOnce = args.Contains("--once");

        int? intervalOverride = null;
        for (var i = 1; i < args.Count - 1; i++)
        {
            if (args[i] == "--interval" && int.TryParse(args[i + 1], out var iv))
            {
                intervalOverride = iv;
                break;
            }
        }

        var configResult = configLoader.Load(GetConfigPath(args));
        if (!configResult.IsValid || configResult.Config is null)
        {
            return new CommandResult(1, string.Empty, string.Join(Environment.NewLine, configResult.Errors) + Environment.NewLine);
        }

        var config = configResult.Config;
        if (!string.Equals(config.WorkItems.Provider, "github", StringComparison.OrdinalIgnoreCase))
        {
            return new CommandResult(1, string.Empty, $"Unsupported work item provider: {config.WorkItems.Provider}{Environment.NewLine}");
        }

        workItemProvider ??= new GitHubWorkItemProvider(config, commandRunner);
        planningAgent ??= new ClaudeCliPlanningAgent(commandRunner);
        codingAgent ??= CodingAgentFactory.Create(config, commandRunner);
        testRunner ??= new ConfiguredCommandTestRunner(commandRunner);
        safetyChecker ??= new GitSafetyChecker(gitRepository);
        pullRequestProvider ??= new GitHubPullRequestProvider(commandRunner);
        missionRepository ??= new PostgresMissionRepository(config.Database.ConnectionString);
        dependencyResolver ??= new GitHubWorkItemDependencyResolver(config, commandRunner);
        missionEventRecorder ??= new PostgresMissionEventRecorder(config.Database.ConnectionString);
        var runSummaryService = CreateRunSummaryService(config, commandRunner, gitRepository, runSummaryRepository, runSummaryGenerator);
        branchPreparer ??= new MissionBranchPreparer(gitRepository);
        orchestrator ??= new MissionOrchestrator(
            workItemProvider, planningAgent, codingAgent, testRunner,
            safetyChecker, pullRequestProvider, gitRepository, missionRepository, branchPreparer, dependencyResolver, missionEventRecorder, runSummaryService);

        var pollInterval = intervalOverride ?? config.Daemon.PollIntervalSeconds;
        var output = new StringBuilder();

        output.AppendLine("Agent Foreman daemon started.");
        output.AppendLine($"Poll interval: {pollInterval} seconds.");
        output.AppendLine();

        void Emit(string line)
        {
            output.AppendLine(line);
            Console.WriteLine(line);
        }

        Emit("Agent Foreman daemon started.");
        Emit($"Poll interval: {pollInterval} seconds.");
        Emit(string.Empty);

        var cts = new CancellationTokenSource();
        ConsoleCancelEventHandler cancelHandler = (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };
        Console.CancelKeyPress += cancelHandler;

        string? lockFilePath = null;
        try
        {
            var lockDir = Path.Combine(config.Project.RepoPath, ".agent");
            lockFilePath = Path.Combine(lockDir, "agent-foreman.lock");
            Directory.CreateDirectory(lockDir);
            if (File.Exists(lockFilePath))
            {
                return new CommandResult(1, string.Empty,
                    $"Daemon already running. Remove lock file to proceed: {lockFilePath}{Environment.NewLine}");
            }
            File.WriteAllText(lockFilePath, Environment.ProcessId.ToString());
        }
        catch
        {
            lockFilePath = null;
        }

        try
        {
            while (!cts.Token.IsCancellationRequested)
            {
                var timestamp = DateTimeOffset.UtcNow.ToString("O");
                Emit($"[{timestamp}] Checking for work...");

                DaemonTickResult tickResult;
                try
                {
                    tickResult = orchestrator.DaemonTickAsync(
                        new DaemonTickRequest(config),
                        msg => Emit(msg),
                        cts.Token).GetAwaiter().GetResult();
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Emit($"Tick error: {ex.Message}");
                    if (runOnce) break;
                    goto sleep;
                }

                if (tickResult.ActiveMissionSkipped)
                {
                    Emit(tickResult.Message ?? "Active mission in progress. Skipping.");
                }
                else if (tickResult.NoReadyWork)
                {
                    Emit("No ready work items found.");
                }
                else if (tickResult.PullRequestUrl is not null)
                {
                    Emit("Run completed successfully.");
                    Emit($"Pull request: {tickResult.PullRequestUrl}");
                }
                else if (tickResult.Message is not null)
                {
                    Emit(tickResult.Message);
                }

                if (runOnce) break;

                sleep:
                Emit($"Sleeping for {pollInterval} seconds.");

                try
                {
                    Task.Delay(TimeSpan.FromSeconds(pollInterval), cts.Token).GetAwaiter().GetResult();
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
        finally
        {
            Console.CancelKeyPress -= cancelHandler;
            if (lockFilePath is not null && File.Exists(lockFilePath))
            {
                try { File.Delete(lockFilePath); } catch { }
            }
        }

        return new CommandResult(0, output.ToString(), string.Empty);
    }

    private static CommandResult RunCancelCommand(
        IReadOnlyList<string> args,
        IAgentForemanConfigLoader configLoader,
        CoreICommandRunner commandRunner,
        IWorkItemProvider? workItemProvider,
        IMissionRepository? missionRepository,
        IMissionEventRecorder? missionEventRecorder)
    {
        if (args.Count < 2)
        {
            return new CommandResult(1, string.Empty, $"Usage: agent-foreman cancel <missionId>{Environment.NewLine}");
        }

        var missionIdArg = args[1];

        var reason = "Cancelled by user.";
        for (var i = 2; i < args.Count - 1; i++)
        {
            if (args[i] == "--reason")
            {
                reason = args[i + 1];
                break;
            }
        }

        var configResult = configLoader.Load(GetConfigPath(args));
        if (!configResult.IsValid || configResult.Config is null)
        {
            return new CommandResult(1, string.Empty, string.Join(Environment.NewLine, configResult.Errors) + Environment.NewLine);
        }

        var config = configResult.Config;
        if (!string.Equals(config.WorkItems.Provider, "github", StringComparison.OrdinalIgnoreCase))
        {
            return new CommandResult(1, string.Empty, $"Unsupported work item provider: {config.WorkItems.Provider}{Environment.NewLine}");
        }

        workItemProvider ??= new GitHubWorkItemProvider(config, commandRunner);
        missionRepository ??= new PostgresMissionRepository(config.Database.ConnectionString);
        missionEventRecorder ??= new PostgresMissionEventRecorder(config.Database.ConnectionString);

        string externalId;
        Mission? mission = null;

        if (int.TryParse(missionIdArg, out _))
        {
            externalId = missionIdArg;
        }
        else if (missionIdArg.StartsWith("issue-", StringComparison.OrdinalIgnoreCase))
        {
            externalId = missionIdArg["issue-".Length..];
        }
        else
        {
            mission = missionRepository.GetById(missionIdArg);
            if (mission is null)
            {
                return new CommandResult(1, string.Empty, $"Mission not found: {missionIdArg}{Environment.NewLine}");
            }

            externalId = mission.ExternalWorkItemId ?? missionIdArg;
        }

        WorkItem item;
        try
        {
            item = workItemProvider.GetWorkItemAsync(externalId, CancellationToken.None).GetAwaiter().GetResult();
        }
        catch (Exception exception)
        {
            return new CommandResult(1, string.Empty, $"Work item not found: {externalId}. {exception.Message}{Environment.NewLine}");
        }

        if (mission is null)
        {
            var missionId = CreateMissionId(item);
            mission = missionRepository.GetById(missionId);
            if (mission is null)
            {
                return new CommandResult(1, string.Empty, $"Mission not found for work item: {externalId}{Environment.NewLine}");
            }
        }

        if (mission.Status == MissionStatus.PullRequestCreated)
        {
            return new CommandResult(1, string.Empty,
                $"Mission already created a pull request and cannot be cancelled safely.{Environment.NewLine}");
        }

        if (mission.Status == MissionStatus.Cancelled)
        {
            return new CommandResult(0, $"Mission is already cancelled.{Environment.NewLine}", string.Empty);
        }

        var cancelledMission = mission with
        {
            Status = MissionStatus.Cancelled,
            LastError = reason,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
        missionRepository.Save(cancelledMission);
        RecordMissionEvent(
            missionEventRecorder,
            CreateMissionEvent(cancelledMission.Id, externalId, MissionEventType.MissionCancelled, MissionEventLevel.Warning, $"Mission cancelled: {reason}"));

        try
        {
            workItemProvider.AddCommentAsync(item, $"Mission cancelled: {reason}", CancellationToken.None).GetAwaiter().GetResult();
        }
        catch
        {
            // Best-effort: commenting does not fail the cancel
        }

        var output = $"""
            Mission cancelled: {mission.Id}
            Reason: {reason}

            """;
        return new CommandResult(0, output, string.Empty);
    }

    private static CommandResult RunRunOnceCommand(
        IReadOnlyList<string> args,
        IAgentForemanConfigLoader configLoader,
        CoreICommandRunner commandRunner,
        IGitRepository gitRepository,
        IWorkItemProvider? workItemProvider,
        IPlanningAgent? planningAgent,
        ICodingAgent? codingAgent,
        ITestRunner? testRunner,
        ISafetyChecker? safetyChecker,
        IPullRequestProvider? pullRequestProvider,
        IMissionRepository? missionRepository,
        IMissionBranchPreparer? branchPreparer = null,
        IWorkItemDependencyResolver? dependencyResolver = null,
        IMissionEventRecorder? missionEventRecorder = null,
        IRunSummaryRepository? runSummaryRepository = null,
        IRunSummaryGenerator? runSummaryGenerator = null)
    {
        var configResult = configLoader.Load(GetConfigPath(args));
        if (!configResult.IsValid || configResult.Config is null)
        {
            return new CommandResult(1, string.Empty, string.Join(Environment.NewLine, configResult.Errors) + Environment.NewLine);
        }

        var config = configResult.Config;
        if (!string.Equals(config.WorkItems.Provider, "github", StringComparison.OrdinalIgnoreCase))
        {
            return new CommandResult(1, string.Empty, $"Unsupported work item provider: {config.WorkItems.Provider}{Environment.NewLine}");
        }

        workItemProvider ??= new GitHubWorkItemProvider(config, commandRunner);
        planningAgent ??= new ClaudeCliPlanningAgent(commandRunner);
        codingAgent ??= CodingAgentFactory.Create(config, commandRunner);
        testRunner ??= new ConfiguredCommandTestRunner(commandRunner);
        safetyChecker ??= new GitSafetyChecker(gitRepository);
        pullRequestProvider ??= new GitHubPullRequestProvider(commandRunner);
        missionRepository ??= new PostgresMissionRepository(config.Database.ConnectionString);
        dependencyResolver ??= new GitHubWorkItemDependencyResolver(config, commandRunner);
        missionEventRecorder ??= new PostgresMissionEventRecorder(config.Database.ConnectionString);
        var runSummaryService = CreateRunSummaryService(config, commandRunner, gitRepository, runSummaryRepository, runSummaryGenerator);

        branchPreparer ??= new MissionBranchPreparer(gitRepository);
        var orchestrator = new MissionOrchestrator(
            workItemProvider, planningAgent, codingAgent, testRunner, safetyChecker,
            pullRequestProvider, gitRepository, missionRepository, branchPreparer, dependencyResolver, missionEventRecorder, runSummaryService);

        var output = new StringBuilder();
        RunOnceResult result;
        try
        {
            result = orchestrator.RunOnceAsync(
                new RunOnceRequest(config),
                msg => output.AppendLine(msg),
                CancellationToken.None).GetAwaiter().GetResult();
        }
        catch (Exception exception)
        {
            return new CommandResult(1, output.ToString(), $"Run-once failed: {exception.Message}{Environment.NewLine}");
        }

        if (result.NoReadyWorkItems)
        {
            output.AppendLine("No ready work items found.");
            return new CommandResult(0, output.ToString(), string.Empty);
        }

        if (result.AllReadyItemsBlocked)
        {
            output.AppendLine("Ready work items found, but all are blocked by dependencies.");
            return new CommandResult(0, output.ToString(), string.Empty);
        }

        if (result.Success)
        {
            output.AppendLine($"Pull request created: {result.PullRequestUrl}");
            return new CommandResult(0, output.ToString(), string.Empty);
        }

        if (result.QuotaDetected)
        {
            return new CommandResult(1, output.ToString(), $"Quota detected. Retry after: {result.RetryAfter:O}{Environment.NewLine}");
        }

        return new CommandResult(1, output.ToString(), $"{result.ErrorMessage}{Environment.NewLine}");
    }

    private static string BuildPrBody(WorkItem item, string outputDirectory)
    {
        var planPath = Path.Combine(outputDirectory, "plan.md");
        var execLogPath = Path.Combine(outputDirectory, "codex-exec.log");
        var testsLogPath = Path.Combine(outputDirectory, "tests.log");

        var lines = new List<string>
        {
            $"## Issue #{item.ExternalId}: {item.Title}",
            string.Empty,
        };

        if (!string.IsNullOrWhiteSpace(item.Url))
        {
            lines.Add($"Issue: {item.Url}");
            lines.Add(string.Empty);
        }

        lines.Add($"Closes #{item.ExternalId}");
        lines.Add(string.Empty);
        lines.Add($"This PR closes #{item.ExternalId} when merged.");
        lines.Add("This pull request was generated by **Agent Foreman** and requires human review before merging.");
        lines.Add(string.Empty);
        lines.Add("### Artifacts");
        lines.Add($"- Plan: `{planPath}`");
        lines.Add($"- Codex log: `{execLogPath}`");

        if (File.Exists(testsLogPath))
        {
            lines.Add($"- Tests log: `{testsLogPath}`");
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static RunSummaryService CreateRunSummaryService(
        AgentForemanConfig config,
        CoreICommandRunner commandRunner,
        IGitRepository gitRepository,
        IRunSummaryRepository? repository,
        IRunSummaryGenerator? generator)
    {
        repository ??= new PostgresRunSummaryRepository(config.Database.ConnectionString);
        generator ??= new ClaudeCliRunSummaryGenerator(commandRunner, config.Planner.Command, config.Planner.Model);
        return new RunSummaryService(generator, repository, gitRepository);
    }

    private static IReadOnlyList<RunSummaryType> GetSummaryTypesForMission(MissionStatus status)
    {
        return status switch
        {
            MissionStatus.PullRequestCreated or MissionStatus.Completed => [RunSummaryType.SuccessSummary],
            MissionStatus.PausedQuota => [RunSummaryType.ResumeContext],
            MissionStatus.Failed or MissionStatus.TestsFailed => [RunSummaryType.FailureSummary, RunSummaryType.ResumeContext],
            _ => [RunSummaryType.ResumeContext],
        };
    }

    private static Mission? FindMissionByExternalId(IMissionRepository missionRepository, string externalId)
    {
        foreach (var status in Enum.GetValues<MissionStatus>())
        {
            var mission = missionRepository
                .GetByStatus(status, 200)
                .FirstOrDefault(item => string.Equals(item.ExternalWorkItemId, externalId, StringComparison.OrdinalIgnoreCase));

            if (mission is not null)
            {
                return mission;
            }
        }

        return null;
    }

    private static IReadOnlyList<string> GenerateMissionSummaries(
        RunSummaryService runSummaryService,
        Mission mission,
        WorkItem item,
        string repoPath,
        string outputDirectory,
        string? currentGitDiff = null)
    {
        try
        {
            var summaries = runSummaryService.GenerateAndSaveAsync(
                new RunSummaryGenerationRequest(
                    mission,
                    repoPath,
                    outputDirectory,
                    item.Title,
                    item.Body,
                    mission.Status,
                    GetSummaryTypesForMission(mission.Status),
                    mission.PullRequestUrl,
                    currentGitDiff),
                CancellationToken.None).GetAwaiter().GetResult();

            return summaries
                .Select(summary => summary.Path)
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Cast<string>()
                .ToList();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    private static string FormatDependencyStatus(WorkItemDependencyStatus status)
    {
        return status switch
        {
            WorkItemDependencyStatus.Satisfied => "closed",
            WorkItemDependencyStatus.Unsatisfied => "open",
            _ => "unknown",
        };
    }

    private static string FormatDependencyResolution(WorkItemDependencyStatus status)
    {
        return status switch
        {
            WorkItemDependencyStatus.Satisfied => "satisfied",
            WorkItemDependencyStatus.Unsatisfied => "unsatisfied",
            _ => "unknown",
        };
    }

    private static CommandResult RunVerifyCommand(
        IReadOnlyList<string> args,
        IAgentForemanConfigLoader configLoader,
        CoreICommandRunner commandRunner,
        IGitRepository gitRepository,
        IWorkItemProvider? workItemProvider,
        IMissionRepository? missionRepository,
        ITestRunner? testRunner,
        ISafetyChecker? safetyChecker,
        IMissionEventRecorder? missionEventRecorder,
        IRunSummaryRepository? runSummaryRepository = null,
        IRunSummaryGenerator? runSummaryGenerator = null)
    {
        if (args.Count < 2)
        {
            return new CommandResult(1, string.Empty, $"Usage: agent-foreman verify <workItemId>{Environment.NewLine}");
        }

        var configResult = configLoader.Load(GetConfigPath(args));
        if (!configResult.IsValid || configResult.Config is null)
        {
            return new CommandResult(1, string.Empty, string.Join(Environment.NewLine, configResult.Errors) + Environment.NewLine);
        }

        var config = configResult.Config;
        if (!string.Equals(config.WorkItems.Provider, "github", StringComparison.OrdinalIgnoreCase))
        {
            return new CommandResult(1, string.Empty, $"Unsupported work item provider: {config.WorkItems.Provider}{Environment.NewLine}");
        }

        workItemProvider ??= new GitHubWorkItemProvider(config, commandRunner);
        missionRepository ??= new PostgresMissionRepository(config.Database.ConnectionString);
        testRunner ??= new ConfiguredCommandTestRunner(commandRunner);
        safetyChecker ??= new GitSafetyChecker(gitRepository);
        missionEventRecorder ??= new PostgresMissionEventRecorder(config.Database.ConnectionString);
        var runSummaryService = CreateRunSummaryService(config, commandRunner, gitRepository, runSummaryRepository, runSummaryGenerator);

        WorkItem item;
        try
        {
            item = workItemProvider.GetWorkItemAsync(args[1], CancellationToken.None).GetAwaiter().GetResult();
        }
        catch (Exception exception)
        {
            return new CommandResult(1, string.Empty, $"Work item not found: {args[1]}. {exception.Message}{Environment.NewLine}");
        }

        var now = DateTimeOffset.UtcNow;
        var missionId = CreateMissionId(item);
        var existingMission = missionRepository.GetById(missionId);
        var testingMission = (existingMission ?? new Mission(
                missionId,
                item.ExternalId,
                item.Source.ToString(),
                item.Title,
                MissionStatus.New,
                Branch: null,
                PlanPath: null,
                PullRequestUrl: null,
                RetryAfter: null,
                LastError: null,
                CreatedAt: now,
                UpdatedAt: now))
            with
            {
                Title = item.Title,
                Status = MissionStatus.Testing,
                LastError = null,
                UpdatedAt = now,
            };
        missionRepository.Save(testingMission);
        RecordMissionEvent(
            missionEventRecorder,
            CreateMissionEvent(testingMission.Id, item.ExternalId, MissionEventType.VerificationStarted, MissionEventLevel.Info, "Running safety checks and tests."));

        var safetyResult = safetyChecker.CheckAsync(config.Project.RepoPath, config.Safety, CancellationToken.None).GetAwaiter().GetResult();
        if (!safetyResult.Passed)
        {
            var violationMessages = string.Join(Environment.NewLine, safetyResult.Violations.Select(v => $"  - {v.Message}"));
            var failedMission = testingMission with
            {
                Status = MissionStatus.Failed,
                LastError = $"Safety check failed:{Environment.NewLine}{violationMessages}",
                UpdatedAt = DateTimeOffset.UtcNow,
            };
            missionRepository.Save(failedMission);
            RecordMissionEvent(
                missionEventRecorder,
                CreateMissionEvent(failedMission.Id, item.ExternalId, MissionEventType.VerificationFailed, MissionEventLevel.Error, "Safety check failed."));
            GenerateMissionSummaries(runSummaryService, failedMission, item, config.Project.RepoPath, Path.Combine(config.Project.RepoPath, ".agent", "runs", $"issue-{SanitizePathSegment(item.ExternalId)}"));
            return new CommandResult(1, string.Empty,
                $"Safety check failed:{Environment.NewLine}{violationMessages}{Environment.NewLine}");
        }

        var outputDirectory = Path.Combine(config.Project.RepoPath, ".agent", "runs", $"issue-{SanitizePathSegment(item.ExternalId)}");
        var testRequest = new TestRunRequest(
            item.ExternalId,
            config.Project.RepoPath,
            config.Tests.Commands,
            outputDirectory);

        TestRunResult testResult;
        try
        {
            testResult = testRunner.RunAsync(testRequest, CancellationToken.None).GetAwaiter().GetResult();
        }
        catch (Exception exception)
        {
            var failedMission = testingMission with
            {
                Status = MissionStatus.TestsFailed,
                LastError = exception.Message,
                UpdatedAt = DateTimeOffset.UtcNow,
            };
            missionRepository.Save(failedMission);
            RecordMissionEvent(
                missionEventRecorder,
                CreateMissionEvent(failedMission.Id, item.ExternalId, MissionEventType.VerificationFailed, MissionEventLevel.Error, $"Tests failed: {exception.Message}"));
            GenerateMissionSummaries(runSummaryService, failedMission, item, config.Project.RepoPath, outputDirectory);
            return new CommandResult(1, string.Empty, $"Tests failed: {exception.Message}{Environment.NewLine}");
        }

        if (testResult.Success)
        {
            var completedMission = testingMission with
            {
                Status = MissionStatus.TestsPassed,
                LastError = null,
                UpdatedAt = DateTimeOffset.UtcNow,
            };
            missionRepository.Save(completedMission);
            RecordMissionEvent(
                missionEventRecorder,
                CreateMissionEvent(
                    completedMission.Id,
                    item.ExternalId,
                    MissionEventType.VerificationCompleted,
                    MissionEventLevel.Info,
                    "Verification passed.",
                    $$"""{"logPath":"{{EscapeJson(testResult.LogPath)}}"}"""));
            return new CommandResult(0,
                $"""
                Verification passed.
                Log path: {testResult.LogPath}

                """,
                string.Empty);
        }

        var failedTestingMission = testingMission with
        {
            Status = MissionStatus.TestsFailed,
            LastError = testResult.ErrorMessage,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
        missionRepository.Save(failedTestingMission);
        RecordMissionEvent(
            missionEventRecorder,
            CreateMissionEvent(
                failedTestingMission.Id,
                item.ExternalId,
                MissionEventType.VerificationFailed,
                MissionEventLevel.Error,
                $"Verification failed: {testResult.ErrorMessage ?? "One or more test commands failed."}"));
        GenerateMissionSummaries(runSummaryService, failedTestingMission, item, config.Project.RepoPath, outputDirectory);

        var failedCmd = testResult.CommandResults.FirstOrDefault(r => !r.Success);
        var errorDetail = new StringBuilder();
        errorDetail.AppendLine($"Tests failed: {testResult.ErrorMessage ?? "One or more test commands failed."}");
        if (failedCmd is not null)
        {
            if (!string.IsNullOrWhiteSpace(failedCmd.Stdout))
            {
                errorDetail.AppendLine("Output:");
                errorDetail.AppendLine(failedCmd.Stdout.TrimEnd());
            }
            if (!string.IsNullOrWhiteSpace(failedCmd.Stderr))
            {
                errorDetail.AppendLine("Stderr:");
                errorDetail.AppendLine(failedCmd.Stderr.TrimEnd());
            }
        }
        errorDetail.AppendLine($"Log: {testResult.LogPath}");
        return new CommandResult(1, string.Empty, errorDetail.ToString());
    }
}
