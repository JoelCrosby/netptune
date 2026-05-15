namespace Netptune.Core.Models.Search;

public record SprintSearchDocument
{
    public required string Id { get; init; }

    public required int SprintId { get; init; }

    public required string Name { get; init; }

    public string? Goal { get; init; }

    public required string WorkspaceSlug { get; init; }

    public required int ProjectId { get; init; }

    public required string Status { get; init; }

    public DateTime UpdatedAt { get; init; }
}
