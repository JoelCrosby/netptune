namespace Netptune.Core.SourceControl.Models;

public sealed record ScmRepository
{
    public required ScmRepositoryRef Ref { get; init; }

    public required string FullName { get; init; }

    public required string DefaultBranch { get; init; }

    public required string WebUrl { get; init; }

    public bool IsPrivate { get; init; }

    public bool IsArchived { get; init; }
}
