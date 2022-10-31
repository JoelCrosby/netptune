using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Netptune.Core.Requests;

public record MoveTasksToGroupRequest
{
    [Required]
    public string BoardId { get; set; } = null!;

    [Required]
    public List<int> TaskIds { get; set; } = null!;

    [Required]
    public int? NewGroupId { get; set; }
}
