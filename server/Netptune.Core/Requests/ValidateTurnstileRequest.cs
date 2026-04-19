using System.ComponentModel.DataAnnotations;

namespace Netptune.Core.Requests;

public record ValidateTurnstileRequest
{
    [Required]
    public string Token { get; set; } = null!;
}
