namespace Netptune.Core.Models.Search;

public record SprintSearchDocument
{
    public required string Id { get; init; }

    public required int SprintId { get; init; }

    public required string Name { get; init; }

    // Documents indexed before sprints had identifiers lack one until the next reindex.
    public string? Identifier { get; init; }

    public string? Goal { get; init; }

    public required string WorkspaceSlug { get; init; }

    public required int ProjectId { get; init; }

    public required string Status { get; init; }

    public DateTime UpdatedAt { get; init; }
}
