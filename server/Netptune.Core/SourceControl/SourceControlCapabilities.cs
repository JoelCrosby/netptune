namespace Netptune.Core.SourceControl;

[Flags]
public enum SourceControlCapabilities
{
    None = 0,
    ListRepositories = 1 << 0,
    ReadBranches = 1 << 1,
    ReadCommits = 1 << 2,
    ReadPullRequests = 1 << 3,
    ReadCheckRuns = 1 << 4,
    CreateBranch = 1 << 5,
    Webhooks = 1 << 6,
    IssueSync = 1 << 7,
}
