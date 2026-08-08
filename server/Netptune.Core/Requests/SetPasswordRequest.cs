using System.ComponentModel.DataAnnotations;

namespace Netptune.Core.Requests;

public record SetPasswordRequest
{
    [Required]
    public string Password { get; set; } = null!;
}
