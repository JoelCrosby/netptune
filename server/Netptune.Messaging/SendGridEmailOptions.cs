using System.ComponentModel.DataAnnotations;

using Netptune.Core.Models.Messaging;

namespace Netptune.Messaging;

public class SendGridEmailOptions : EmailOptions
{
    [Required]
    public string SendGridApiKey { get; set; } = null!;
}
