using Netptune.Core.Entities;
using Netptune.Core.Enums;

namespace Netptune.Core.Events
{
    public class IActivity
    {
        EntityType EntityType { get; }

        string UserId { get; }

        ActivityType Type { get; }
    }
}
