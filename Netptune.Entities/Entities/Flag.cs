using System.ComponentModel.DataAnnotations;
using Netptune.Entities.Entites.BaseEntities;

namespace Netptune.Entities.Entites
{
    public class Flag : AuditableEntity<int>
    {
        public string Name { get; set; }

        public string Description { get; set; }
    }
}
