using CoreCommandOutputLine = AgentForeman.Core.Commands.CommandOutputLine;
using CoreCommandOutputStream = AgentForeman.Core.Commands.CommandOutputStream;
using CoreCommandRequest = AgentForeman.Core.Commands.CommandRequest;
using CoreICommandRunner = AgentForeman.Core.Commands.ICommandRunner;
using AgentForeman.Core.Configuration;
using AgentForeman.Core.Git;
using AgentForeman.Core.Planning;
using AgentForeman.Core.Prerequisites;
using AgentForeman.Core.State;
using AgentForeman.Core.WorkItems;
using AgentForeman.Infrastructure.Commands;
using AgentForeman.Infrastructure.Configuration;
using AgentForeman.Infrastructure.Git;
using AgentForeman.Infrastructure.Planning;
using AgentForeman.Infrastructure.Prerequisites;
using AgentForeman.Infrastructure.State;
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
        IMissionRepository? missionRepository = null)
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

        if (args[0] == "work-items")
        {
            return RunWorkItemsCommand(args, configLoader, commandRunner);
        }

        if (args[0] == "plan")
        {
            return RunPlanCommand(args, configLoader, commandRunner, workItemProvider, planningAgent, missionRepository);
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
        CoreICommandRunner commandRunner)
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

        var provider = new GitHubWorkItemProvider(configResult.Config, commandRunner);

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

    private static CommandResult RunPlanCommand(
        IReadOnlyList<string> args,
        IAgentForemanConfigLoader configLoader,
        CoreICommandRunner commandRunner,
        IWorkItemProvider? workItemProvider,
        IPlanningAgent? planningAgent,
        IMissionRepository? missionRepository)
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
            return new CommandResult(1, string.Empty, $"Planning failed: {exception.Message}{Environment.NewLine}");
        }

        if (planResult.Success)
        {
            missionRepository.Save(planningMission with
            {
                Status = MissionStatus.PlanReady,
                PlanPath = planResult.PlanPath,
                LastError = null,
                UpdatedAt = DateTimeOffset.UtcNow,
            });
            return new CommandResult(0, $"Plan created: {planResult.PlanPath}{Environment.NewLine}", string.Empty);
        }

        if (IsQuotaFailure(planResult, config))
        {
            var retryAfter = DateTimeOffset.UtcNow.AddHours(config.Quota.RetryAfterHours ?? 1);
            var reason = "Claude quota or rate limit detected while creating the plan.";
            missionRepository.Save(planningMission with
            {
                Status = MissionStatus.PausedQuota,
                PlanPath = planResult.PlanPath,
                RetryAfter = retryAfter,
                LastError = planResult.ErrorMessage,
                UpdatedAt = DateTimeOffset.UtcNow,
            });
            workItemProvider.MarkAsPausedAsync(item, reason, retryAfter, CancellationToken.None).GetAwaiter().GetResult();
            return new CommandResult(1, string.Empty, $"{reason} Retry after: {retryAfter:O}{Environment.NewLine}");
        }

        missionRepository.Save(planningMission with
        {
            Status = MissionStatus.Failed,
            PlanPath = planResult.PlanPath,
            LastError = planResult.ErrorMessage,
            UpdatedAt = DateTimeOffset.UtcNow,
        });
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
}
