using System.ComponentModel.DataAnnotations;

namespace Netptune.Core.Requests;

public record AddTasksToSprintRequest
{
    [MaxLength(RequestLimits.MaxBulkIds)]
    public List<int> TaskIds { get; init; } = new();
}
