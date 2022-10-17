using System.ComponentModel.DataAnnotations;

namespace Netptune.Services.Authentication;

public class NetptuneAuthenticationOptions
{
    [Required]
    public string Issuer { get; set; } = null!;

    [Required]
    public string Audience { get; set; } = null!;

    [Required]
    public string SecurityKey { get; set; } = null!;

    [Required]
    public string GitHubClientId { get; set; } = null!;

    [Required]
    public string GitHubSecret { get; set; } = null!;
}
