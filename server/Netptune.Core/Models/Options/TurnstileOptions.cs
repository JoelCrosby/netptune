using System.ComponentModel.DataAnnotations;

namespace Netptune.Core.Models.Options;

public sealed class TurnstileOptions
{
    public const string SectionName = "Turnstile";

    [Required]
    public string SecretKey { get; set; } = null!;
}
