using System.ComponentModel.DataAnnotations;

namespace Netptune.Core.Requests;

public class RequestPasswordResetRequest
{
    [Required]
    public string Email { get; set; } = null!;
}
