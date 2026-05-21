using AgentForeman.Core.Prerequisites;

namespace AgentForeman.Infrastructure.Prerequisites;

public sealed class FileSystemRepositoryChecker : IRepositoryChecker
{
    public bool DirectoryExists(string path)
    {
        return Directory.Exists(path);
    }

    public bool IsGitRepository(string path)
    {
        return Directory.Exists(Path.Combine(path, ".git")) || File.Exists(Path.Combine(path, ".git"));
    }
}
