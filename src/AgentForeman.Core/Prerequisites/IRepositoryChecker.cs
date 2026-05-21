namespace AgentForeman.Core.Prerequisites;

public interface IRepositoryChecker
{
    bool DirectoryExists(string path);
    bool IsGitRepository(string path);
}
