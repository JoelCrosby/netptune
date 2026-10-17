namespace Netptune.Core.SourceControl.Models;

public sealed record ScmCommit
{
    public required string Sha { get; init; }

    public required string Message { get; init; }

    public required ScmIdentity Author { get; init; }

    public DateTimeOffset AuthoredAt { get; init; }

    public required string WebUrl { get; init; }
}
