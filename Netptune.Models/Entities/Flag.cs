using Netptune.Entities.Entites.BaseEntities;
using System.ComponentModel.DataAnnotations;

namespace Netptune.Entities.Entites
{
    public class Flag : AuditableEntity<int>
    {

        [Required]
        [StringLength(128)]
        public string Name { get; set; }

        [StringLength(1024)]
        public string Description { get; set; }

    }
}
