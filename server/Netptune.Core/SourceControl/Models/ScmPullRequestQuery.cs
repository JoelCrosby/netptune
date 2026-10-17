namespace Netptune.Core.SourceControl.Models;

public sealed record ScmPullRequestQuery(string? SourceBranch, bool IncludeClosed, int Take);
