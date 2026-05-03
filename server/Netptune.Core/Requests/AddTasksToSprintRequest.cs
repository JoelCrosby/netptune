namespace Netptune.Core.Requests;

public record AddTasksToSprintRequest
{
    public List<int> TaskIds { get; init; } = new();
}
