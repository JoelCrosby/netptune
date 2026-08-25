using System.ComponentModel.DataAnnotations;

namespace Netptune.Core.Requests;

public record AddTaskToBoardRequest
{
    [Required]
    public int BoardId { get; set; }

    public int? BoardGroupId { get; set; }
}
