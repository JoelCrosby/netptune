namespace Netptune.Core.Requests;

public sealed class TaskFilter : PageRequest
{
    public int? ProjectId { get; init; }

    public int? SprintId { get; init; }

    public int? ExcludeSprintId { get; init; }

    public string? Search { get; init; }

    public string[] Tags { get; init; } = [];

    public int[] StatusIds { get; init; } = [];

    public string[] Assignees { get; init; } = [];

    public bool? NoSprint { get; init; }
}
