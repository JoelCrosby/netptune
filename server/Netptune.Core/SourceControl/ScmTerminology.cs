namespace Netptune.Core.SourceControl;

public sealed record ScmTerminology
{
    public required string PullRequest { get; init; }

    public required string PullRequests { get; init; }

    public required string Issue { get; init; }

    public required string CheckRuns { get; init; }
}
