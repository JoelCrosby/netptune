using System.ComponentModel.DataAnnotations;

namespace Netptune.Core.Requests;

public record UpdateTagRequest
{
    [Required]
    [MinLength(2)]
    [MaxLength(128)]
    public string CurrentValue { get; set; } = null!;

    [Required]
    [MinLength(2)]
    [MaxLength(128)]
    public string NewValue { get; set; } = null!;
}
