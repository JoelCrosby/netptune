using Netptune.Core.Enums;

namespace Netptune.Core.Events
{
    public class ActivityEvent : IActivityEvent
    {
        public EntityType EntityType { get; set; }

        public string UserId { get; set; }

        public ActivityType Type { get; set; }

        public int? EntityId { get; set; }
    }
}
