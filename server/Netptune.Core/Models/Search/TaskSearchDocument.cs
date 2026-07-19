namespace Netptune.Core.Models.Search;

public record TaskSearchDocument
{
    public required string Id { get; init; }

    public required int TaskId { get; init; }

    public required string Title { get; init; }

    public string? Description { get; init; }

    public required string WorkspaceSlug { get; init; }

    public required string Status { get; init; }

    public string? Priority { get; init; }

    public List<string> AssigneeIds { get; init; } = [];

    public List<int> TagIds { get; init; } = [];

    public int? ProjectId { get; init; }

    public int ProjectScopeId { get; init; }

    public DateTime UpdatedAt { get; init; }
}
