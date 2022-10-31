using System.ComponentModel.DataAnnotations;

namespace Netptune.Core.Requests;

public record DeleteTagFromTaskRequest
{
    [Required]
    public string SystemId { get; set; } = null!;

    [Required]
    public string Tag { get; set; } = null!;
}
