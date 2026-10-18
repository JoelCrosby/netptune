namespace Netptune.Core.SourceControl.Models;

public sealed record ScmPullRequestFile
{
    public required string Path { get; init; }

    public required string Status { get; init; }

    public int Additions { get; init; }

    public int Deletions { get; init; }

    public string? Patch { get; init; }
}
