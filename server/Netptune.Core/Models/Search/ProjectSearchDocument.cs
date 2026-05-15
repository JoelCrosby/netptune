namespace Netptune.Core.Models.Search;

public record ProjectSearchDocument
{
    public required string Id { get; init; }

    public required int ProjectId { get; init; }

    public required string Name { get; init; }

    public string? Description { get; init; }

    public required string Key { get; init; }

    public required string WorkspaceSlug { get; init; }

    public DateTime UpdatedAt { get; init; }
}
