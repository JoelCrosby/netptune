namespace Netptune.Core.Models.Search;

public record PageSearchDocument
{
    public required string Id { get; init; }

    public required int PageId { get; init; }

    public required string Slug { get; init; }

    public required string Title { get; init; }

    // The titles of the folders above the page, outermost first, joined with " / ".
    public string? Path { get; init; }

    public string? Body { get; init; }

    public required string Kind { get; init; }

    public required string Status { get; init; }

    public List<string> Tags { get; init; } = [];

    public required string WorkspaceSlug { get; init; }

    public int? ProjectId { get; init; }

    public DateTime UpdatedAt { get; init; }
}
