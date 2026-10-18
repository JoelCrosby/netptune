namespace Netptune.Core.SourceControl.Models;

public sealed record ScmTreeEntry(string Path, bool IsDirectory, long? Size);

public sealed record ScmTree
{
    public required string CommitSha { get; init; }

    public required IReadOnlyList<ScmTreeEntry> Entries { get; init; }

    public bool IsTruncated { get; init; }
}
