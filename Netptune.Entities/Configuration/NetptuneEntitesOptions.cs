using System.ComponentModel.DataAnnotations;

namespace Netptune.Entities.Configuration
{
    public class NetptuneEntitesOptions
    {
        [Required]
        public string ConnectionString { get; set; }

        public bool IsWindows { get; set; } = true;
    }
}
