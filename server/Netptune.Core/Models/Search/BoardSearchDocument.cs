namespace Netptune.Core.Models.Search;

public record BoardSearchDocument
{
    public required string Id { get; init; }

    public required int BoardId { get; init; }

    public required string Name { get; init; }

    public required string WorkspaceSlug { get; init; }

    public required int ProjectId { get; init; }

    public required string Identifier { get; init; }

    public DateTime UpdatedAt { get; init; }
}
