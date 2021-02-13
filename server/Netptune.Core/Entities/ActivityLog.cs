using Netptune.Core.BaseEntities;
using Netptune.Core.Enums;

namespace Netptune.Core.Entities
{
    public class ActivityLog : AuditableEntity<int>
    {
        public EntityType EntityType { get; set; }

        public string UserId { get; set; }

        public ActivityType Type { get; set; }
    }
}
