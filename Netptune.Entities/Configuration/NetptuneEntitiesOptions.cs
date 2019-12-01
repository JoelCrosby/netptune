using System.ComponentModel.DataAnnotations;

namespace Netptune.Entities.Configuration
{
    public class NetptuneEntitiesOptions
    {
        [Required]
        public string ConnectionString { get; set; }
    }
}
