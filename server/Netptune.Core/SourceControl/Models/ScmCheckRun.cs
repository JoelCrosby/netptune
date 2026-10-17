namespace Netptune.Core.SourceControl.Models;

public sealed record ScmCheckRun
{
    public required string ExternalId { get; init; }

    public required string Name { get; init; }

    public ScmCheckRunStatus Status { get; init; }

    public ScmCheckRunConclusion? Conclusion { get; init; }

    public required string CommitSha { get; init; }

    public required string WebUrl { get; init; }

    public DateTimeOffset? CompletedAt { get; init; }
}
