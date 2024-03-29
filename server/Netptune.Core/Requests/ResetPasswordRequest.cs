using System.ComponentModel.DataAnnotations;

namespace Netptune.Core.Requests;

public record ResetPasswordRequest
{
    [Required]
    public string UserId { get; set; } = null!;

    [Required]
    public string Code { get; set; } = null!;

    [Required]
    public string Password { get; set; }  = null!;
}
