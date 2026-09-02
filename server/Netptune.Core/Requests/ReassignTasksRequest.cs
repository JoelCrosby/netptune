using System.ComponentModel.DataAnnotations;

namespace Netptune.Core.Requests;

// An empty AssigneeIds clears every assignee from the tasks rather than naming new ones.
public record ReassignTasksRequest
{
    [Required]
    public string BoardId { get; set; } = null!;

    [Required]
    public List<int> TaskIds { get; set; } = null!;

    public List<string> AssigneeIds { get; set; } = [];
}
