namespace Netptune.Core.SourceControl.Models;

public sealed record ScmCommitQuery(string? Branch, DateTimeOffset? Since, int Take);
