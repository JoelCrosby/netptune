using System.ComponentModel.DataAnnotations;

namespace Netptune.Core.Models.Messaging;

public class EmailOptions
{
    [Required]
    public string DefaultFromAddress { get; set; }

    [Required]
    public string DefaultFromDisplayName { get; set; }
}