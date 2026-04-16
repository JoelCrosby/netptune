using System.ComponentModel.DataAnnotations;

namespace Netptune.Core.Requests;

public record ToggleUserPermissionRequest
{
    [Required]
    public string UserId { get; set; } = null!;

    [Required]
    public string Permission { get; set; } = null!;
}
