using System.ComponentModel.DataAnnotations;

using Netptune.Core.Models.Messaging;

namespace Netptune.Messaging;

public class CloudflareEmailOptions : EmailOptions
{
    public const string CloudflareSectionName = "Email:Cloudflare";

    [Required]
    public string ApiToken { get; set; } = null!;

    [Required]
    public string AccountId { get; set; } = null!;
}
