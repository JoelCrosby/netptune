namespace Netptune.Core.ViewModels.Pages;

public sealed record PageBacklinkViewModel
{
    public int PageId { get; init; }

    public required string Title { get; init; }

    public required string Slug { get; init; }

    public string? Snippet { get; init; }
}
