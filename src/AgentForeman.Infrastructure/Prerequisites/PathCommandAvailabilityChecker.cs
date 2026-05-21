using AgentForeman.Core.Prerequisites;

namespace AgentForeman.Infrastructure.Prerequisites;

public sealed class PathCommandAvailabilityChecker : ICommandAvailabilityChecker
{
    public bool IsAvailable(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            return false;
        }

        var pathValue = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(pathValue))
        {
            return false;
        }

        foreach (var directory in pathValue.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            if (File.Exists(Path.Combine(directory, command)))
            {
                return true;
            }

            if (OperatingSystem.IsWindows())
            {
                foreach (var extension in GetWindowsExecutableExtensions())
                {
                    if (File.Exists(Path.Combine(directory, command + extension)))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private static IEnumerable<string> GetWindowsExecutableExtensions()
    {
        var pathExtensions = Environment.GetEnvironmentVariable("PATHEXT");
        return string.IsNullOrWhiteSpace(pathExtensions)
            ? new[] { ".exe", ".cmd", ".bat" }
            : pathExtensions.Split(';', StringSplitOptions.RemoveEmptyEntries);
    }
}
