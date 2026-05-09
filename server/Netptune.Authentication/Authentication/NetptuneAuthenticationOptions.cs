using System.ComponentModel.DataAnnotations;

namespace Netptune.Authentication;

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

    [Required]
    public string GitHubCallback { get; set; } = null!;

    [Required]
    public string GoogleClientId { get; set; } = null!;

    [Required]
    public string GoogleSecret { get; set; } = null!;

    [Required]
    public string GoogleCallback { get; set; } = null!;

    [Required]
    public string MicrosoftClientId { get; set; } = null!;

    [Required]
    public string MicrosoftSecret { get; set; } = null!;

    [Required]
    public string MicrosoftCallback { get; set; } = null!;
}
