namespace Netptune.Core.Models.Pages;

public sealed record PageContentUpdate
{
    public int PageId { get; init; }

    public int WorkspaceId { get; init; }

    public required string WorkspaceKey { get; init; }

    public int ExpectedRevision { get; init; }

    public required string Title { get; init; }

    public required string Slug { get; init; }

    public string? Body { get; init; }

    public required string UserId { get; init; }

    public int? RestoredFromVersionId { get; init; }
}
