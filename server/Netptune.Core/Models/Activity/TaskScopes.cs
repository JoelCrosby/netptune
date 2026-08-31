namespace Netptune.Core.Models.Activity;

public sealed record TaskScopes
{
    public int? ProjectId { get; init; }

    public int? SprintId { get; init; }

    public IReadOnlyCollection<int> BoardIds { get; init; } = [];

    public IReadOnlyCollection<int> BoardGroupIds { get; init; } = [];
}
