namespace Netptune.Core.SourceControl.Models;

public sealed record ScmPullRequest
{
    public required string ExternalId { get; init; }

    public required string Number { get; init; }

    public required string Title { get; init; }

    public string? Body { get; init; }

    public ScmPullRequestState State { get; init; }

    public required string SourceBranch { get; init; }

    public required string TargetBranch { get; init; }

    public required ScmIdentity Author { get; init; }

    public required string WebUrl { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset UpdatedAt { get; init; }

    public DateTimeOffset? MergedAt { get; init; }

    public DateTimeOffset? ClosedAt { get; init; }
}
