using System.ComponentModel.DataAnnotations;

namespace Netptune.Repositories.Configuration
{
    public class NetptuneRepositoryOptions
    {
        [Required]
        public string ConnectionString { get; set; }
    }
}
