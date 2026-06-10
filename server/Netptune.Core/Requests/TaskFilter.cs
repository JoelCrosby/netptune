using Netptune.Core.Enums;

namespace Netptune.Core.Requests;

public sealed class TaskFilter
{
    public int? ProjectId { get; init; }

    public int? SprintId { get; init; }

    public int? ExcludeSprintId { get; init; }

    public string? Search { get; init; }

    public string[] Tags { get; init; } = [];

    public ProjectTaskStatus[] Statuses { get; init; } = [];

    public string[] Assignees { get; init; } = [];

    public bool? NoSprint { get; init; }

    public int? Page { get; init; }

    public int? PageSize { get; init; }
}
