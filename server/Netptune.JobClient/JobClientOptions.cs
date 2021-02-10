using System.ComponentModel.DataAnnotations;

namespace Netptune.JobClient
{
    public class JobClientOptions
    {
        [Required]
        public string ConnectionString { get; set; }
    }
}
