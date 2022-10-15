using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Netptune.Core.Requests;

public class ReassignTasksRequest
{
    [Required]
    public string BoardId { get; set; } = null!;

    [Required]
    public List<int> TaskIds { get; set; } = null!;

    [Required]
    public string AssigneeId { get; set; } = null!;
}
