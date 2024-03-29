using System.ComponentModel.DataAnnotations;

namespace Netptune.Core.Requests;

public record AddCommentRequest
{
    [Required]
    public string Comment { get; set; } = null!;

    [Required]
    public string SystemId { get; set; } = null!;
}
