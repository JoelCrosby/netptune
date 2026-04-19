using System.ComponentModel.DataAnnotations;

using Netptune.Core.Models.Messaging;

namespace Netptune.Messaging;

public class CloudflareEmailOptions : EmailOptions
{
    [Required]
    public string ApiToken { get; set; } = null!;

    [Required]
    public string AccountId { get; set; } = null!;
}
