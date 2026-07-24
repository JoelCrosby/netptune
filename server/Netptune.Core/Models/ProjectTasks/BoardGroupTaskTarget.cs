namespace Netptune.Core.Models.ProjectTasks;

public sealed record BoardGroupTaskTarget
{
    public required int Id { get; init; }

    public required string Name { get; init; }

    public required double MaxSortOrder { get; init; }

    public int WorkspaceId { get; init; }

    public int? StatusId { get; init; }

    public int? ProjectId { get; init; }
}
