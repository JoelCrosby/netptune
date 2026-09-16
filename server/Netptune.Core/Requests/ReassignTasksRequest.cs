using System.ComponentModel.DataAnnotations;

namespace Netptune.Core.Requests;

public record ReassignTasksRequest
{
    [Required]
    public string BoardId { get; set; } = null!;

    [Required]
    [MaxLength(RequestLimits.MaxBulkIds)]
    public List<int> TaskIds { get; set; } = null!;

    [MaxLength(RequestLimits.MaxBulkAssignees)]
    public List<string> AssigneeIds { get; set; } = [];
}
