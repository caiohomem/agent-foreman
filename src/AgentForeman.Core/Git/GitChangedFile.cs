namespace AgentForeman.Core.Git;

public sealed record GitChangedFile(string StatusCode, string Path)
{
    public bool IsUntracked => StatusCode == "??";
    public bool IsModified => StatusCode.Contains('M');
    public bool IsDeleted => StatusCode.Contains('D');
    public bool IsRenamed => StatusCode.Contains('R');
}
