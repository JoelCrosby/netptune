using System.ComponentModel.DataAnnotations;

using Netptune.Core.Requests;

namespace Netptune.Api.Requests;

public sealed record PublicReassignTasksRequest
{
    [Required]
    public string BoardId { get; init; } = null!;

    [Required]
    [MaxLength(RequestLimits.MaxBulkIds)]
    public List<string> SystemIds { get; init; } = [];

    [MaxLength(RequestLimits.MaxBulkAssignees)]
    public List<string> AssigneeIds { get; init; } = [];

    public ReassignTasksRequest ToRequest(List<int> taskIds)
    {
        return new ReassignTasksRequest
        {
            BoardId = BoardId,
            TaskIds = taskIds,
            AssigneeIds = AssigneeIds,
        };
    }
}
