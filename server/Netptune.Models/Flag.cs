using Netptune.Models.BaseEntities;

namespace Netptune.Models
{
    public class Flag : AuditableEntity<int>
    {
        public string Name { get; set; }

        public string Description { get; set; }
    }
}
