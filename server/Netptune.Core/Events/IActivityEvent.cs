using Netptune.Core.Entities;
using Netptune.Core.Enums;

namespace Netptune.Core.Events
{
    public interface IActivityEvent
    {
        EntityType EntityType { get; }

        string UserId { get; }

        ActivityType Type { get; }

        int EntityId { get; }
    }
}
