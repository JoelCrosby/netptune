using System.ComponentModel.DataAnnotations;

namespace Netptune.Core.Models.Options
{
    public class NetptuneEntitiesOptions
    {
        [Required]
        public string ConnectionString { get; set; }
    }
}
