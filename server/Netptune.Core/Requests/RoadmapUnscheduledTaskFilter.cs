namespace Netptune.Core.Requests;

public sealed class RoadmapUnscheduledTaskFilter : PageRequest
{
    public int[] ProjectIds { get; init; } = [];

    public int[] SprintIds { get; init; } = [];
}
