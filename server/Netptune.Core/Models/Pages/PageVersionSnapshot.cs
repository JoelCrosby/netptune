namespace Netptune.Core.Models.Pages;

public sealed record PageVersionSnapshot
{
    public int PageId { get; init; }

    public int Revision { get; init; }

    public required string Title { get; init; }

    public string? Body { get; init; }

    public required string UserId { get; init; }

    public int? RestoredFromVersionId { get; init; }
}
