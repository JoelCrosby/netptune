using System.ComponentModel.DataAnnotations;

using Netptune.Core.Models.Hosting;

namespace Netptune.Services.Configuration;

public class NetptuneServiceOptions
{
    [Required]
    public HostingOptions HostingOptions { get; set; }
}