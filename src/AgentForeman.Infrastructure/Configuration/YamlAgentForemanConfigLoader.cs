using AgentForeman.Core.Configuration;

namespace AgentForeman.Infrastructure.Configuration;

public sealed class YamlAgentForemanConfigLoader : IAgentForemanConfigLoader
{
    public AgentForemanConfigLoadResult Load(string path)
    {
        if (!File.Exists(path))
        {
            return AgentForemanConfigLoadResult.Failure(new[] { $"Config file not found: {path}" });
        }

        var values = SimpleYaml.Parse(File.ReadAllLines(path));
        var config = new AgentForemanConfig
        {
            Project = new ProjectConfig
            {
                Name = values.GetScalar("project", "name"),
                RepoPath = values.GetScalar("project", "repoPath"),
                DefaultBranch = values.GetScalar("project", "defaultBranch"),
            },
            WorkItems = new WorkItemsConfig
            {
                Provider = values.GetScalar("workItems", "provider"),
                Repo = values.GetScalar("workItems", "repo"),
                ReadyLabel = values.GetScalar("workItems", "readyLabel"),
                WorkingLabel = values.GetScalar("workItems", "workingLabel"),
                ReviewLabel = values.GetScalar("workItems", "reviewLabel"),
                PausedLabel = values.GetScalar("workItems", "pausedLabel"),
                BlockedLabel = values.GetScalarOrDefault("workItems", "blockedLabel", "agent-blocked"),
                FailedLabel = values.GetScalarOrDefault("workItems", "failedLabel", "agent-failed"),
            },
            Planner = new PlannerConfig
            {
                Provider = values.GetScalar("planner", "provider"),
                Command = values.GetScalar("planner", "command"),
            },
            Executor = new ExecutorConfig
            {
                Provider = values.GetScalar("executor", "provider"),
                Command = values.GetScalar("executor", "command"),
                Sandbox = values.GetScalar("executor", "sandbox"),
                Approval = values.GetScalar("executor", "approval"),
            },
            Tests = new TestsConfig
            {
                Commands = values.GetList("tests", "commands"),
            },
            Safety = new SafetyConfig
            {
                MaxFilesChanged = values.GetNullableInt("safety", "maxFilesChanged"),
                ForbiddenPaths = values.GetList("safety", "forbiddenPaths"),
            },
            Quota = new QuotaConfig
            {
                RetryAfterHours = values.GetNullableInt("quota", "retryAfterHours"),
                QuotaPatterns = values.GetList("quota", "quotaPatterns"),
            },
            Database = new DatabaseConfig
            {
                Provider = values.GetScalar("database", "provider"),
                ConnectionString = values.GetScalar("database", "connectionString"),
            },
            Daemon = new DaemonConfig
            {
                Enabled = values.GetNullableBool("daemon", "enabled") ?? true,
                PollIntervalSeconds =
                    values.GetNullableInt("daemon", "pollIntervalSeconds")
                    ?? values.GetNullableInt("daemon", "pollintervalseconds")
                    ?? 300,
                RunOnStartup = values.GetNullableBool("daemon", "runOnStartup") ?? true,
            },
        };

        var errors = Validate(config);
        return errors.Count == 0
            ? AgentForemanConfigLoadResult.Success(config)
            : AgentForemanConfigLoadResult.Failure(errors);
    }

    private static IReadOnlyList<string> Validate(AgentForemanConfig config)
    {
        var errors = new List<string>();

        Require(config.Project.Name, "project.name", errors);
        Require(config.Project.RepoPath, "project.repoPath", errors);
        Require(config.Project.DefaultBranch, "project.defaultBranch", errors);
        Require(config.WorkItems.Provider, "workItems.provider", errors);
        Require(config.Planner.Provider, "planner.provider", errors);
        Require(config.Planner.Command, "planner.command", errors);
        Require(config.Executor.Provider, "executor.provider", errors);
        Require(config.Executor.Command, "executor.command", errors);
        Require(config.Database.Provider, "database.provider", errors);
        Require(config.Database.ConnectionString, "database.connectionString", errors);

        if (!string.IsNullOrWhiteSpace(config.Database.Provider)
            && !string.Equals(config.Database.Provider, "postgresql", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add("database.provider must be postgresql.");
        }

        return errors;
    }

    private static void Require(string value, string fieldName, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add($"{fieldName} is required.");
        }
    }
}

internal sealed class SimpleYamlValues
{
    private readonly Dictionary<string, Dictionary<string, string>> _scalars = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Dictionary<string, List<string>>> _lists = new(StringComparer.Ordinal);

    public void SetScalar(string section, string key, string value)
    {
        if (!_scalars.TryGetValue(section, out var sectionValues))
        {
            sectionValues = new Dictionary<string, string>(StringComparer.Ordinal);
            _scalars[section] = sectionValues;
        }

        sectionValues[key] = value;
    }

    public void AddListItem(string section, string key, string value)
    {
        if (!_lists.TryGetValue(section, out var sectionValues))
        {
            sectionValues = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            _lists[section] = sectionValues;
        }

        if (!sectionValues.TryGetValue(key, out var items))
        {
            items = new List<string>();
            sectionValues[key] = items;
        }

        items.Add(value);
    }

    public string GetScalar(string section, string key)
    {
        return _scalars.TryGetValue(section, out var sectionValues)
            && sectionValues.TryGetValue(key, out var value)
            ? value
            : string.Empty;
    }

    public string GetScalarOrDefault(string section, string key, string defaultValue)
    {
        var value = GetScalar(section, key);
        return string.IsNullOrWhiteSpace(value) ? defaultValue : value;
    }

    public int? GetNullableInt(string section, string key)
    {
        var value = GetScalar(section, key);
        return int.TryParse(value, out var number) ? number : null;
    }

    public bool? GetNullableBool(string section, string key)
    {
        var value = GetScalar(section, key);
        return bool.TryParse(value, out var flag) ? flag : null;
    }

    public IReadOnlyList<string> GetList(string section, string key)
    {
        return _lists.TryGetValue(section, out var sectionValues)
            && sectionValues.TryGetValue(key, out var items)
            ? items
            : Array.Empty<string>();
    }
}

internal static class SimpleYaml
{
    public static SimpleYamlValues Parse(IEnumerable<string> lines)
    {
        var values = new SimpleYamlValues();
        var currentSection = string.Empty;
        var currentListKey = string.Empty;

        foreach (var rawLine in lines)
        {
            if (string.IsNullOrWhiteSpace(rawLine))
            {
                continue;
            }

            var trimmedStart = rawLine.TrimStart();
            if (trimmedStart.StartsWith('#'))
            {
                continue;
            }

            var indent = rawLine.Length - trimmedStart.Length;
            var line = StripInlineComment(trimmedStart);
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            if (indent == 0 && line.EndsWith(':'))
            {
                currentSection = line[..^1].Trim();
                currentListKey = string.Empty;
                continue;
            }

            if (indent == 2 && line.EndsWith(':'))
            {
                currentListKey = line[..^1].Trim();
                continue;
            }

            if (indent == 2)
            {
                var separatorIndex = line.IndexOf(':');
                if (separatorIndex < 0)
                {
                    continue;
                }

                var key = line[..separatorIndex].Trim();
                var value = Unquote(line[(separatorIndex + 1)..].Trim());
                values.SetScalar(currentSection, key, value);
                currentListKey = string.Empty;
                continue;
            }

            if (indent == 4 && line.StartsWith("- ", StringComparison.Ordinal) && currentListKey.Length > 0)
            {
                values.AddListItem(currentSection, currentListKey, Unquote(line[2..].Trim()));
            }
        }

        return values;
    }

    private static string StripInlineComment(string value)
    {
        var inSingleQuote = false;
        var inDoubleQuote = false;

        for (var index = 0; index < value.Length; index++)
        {
            var character = value[index];
            if (character == '\'' && !inDoubleQuote)
            {
                inSingleQuote = !inSingleQuote;
            }
            else if (character == '"' && !inSingleQuote)
            {
                inDoubleQuote = !inDoubleQuote;
            }
            else if (character == '#' && !inSingleQuote && !inDoubleQuote)
            {
                return value[..index].TrimEnd();
            }
        }

        return value.TrimEnd();
    }

    private static string Unquote(string value)
    {
        if (value.Length >= 2
            && ((value[0] == '"' && value[^1] == '"') || (value[0] == '\'' && value[^1] == '\'')))
        {
            return value[1..^1];
        }

        return value;
    }
}
