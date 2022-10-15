using System.ComponentModel.DataAnnotations;

namespace Netptune.Core.Models.Messaging;

public class EmailOptions
{
    [Required]
    public string DefaultFromAddress { get; set; } = null!;

    [Required]
    public string DefaultFromDisplayName { get; set; } = null!;
}
