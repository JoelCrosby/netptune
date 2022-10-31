using System.ComponentModel.DataAnnotations;

namespace Netptune.Core.Requests;

public record RequestPasswordResetRequest
{
    [Required]
    public string Email { get; set; } = null!;
}
