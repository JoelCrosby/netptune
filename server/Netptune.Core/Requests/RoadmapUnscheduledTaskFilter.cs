namespace Netptune.Core.Requests;

public sealed class RoadmapUnscheduledTaskFilter : PageRequest
{
    public int[] ProjectIds { get; init; } = [];

    public int[] SprintIds { get; init; } = [];

    public string? Search { get; init; }

    public string[] Tags { get; init; } = [];

    public int[] StatusIds { get; init; } = [];

    public string[] Assignees { get; init; } = [];
}
