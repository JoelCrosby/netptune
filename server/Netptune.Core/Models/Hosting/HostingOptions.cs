using System.ComponentModel.DataAnnotations;

namespace Netptune.Core.Models.Hosting;

public class HostingOptions
{
    [Required]
    public string ContentRootPath { get; set; }

    [Required]
    public string ClientOrigin { get; set; }
}