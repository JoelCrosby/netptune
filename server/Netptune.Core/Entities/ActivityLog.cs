using System;
using System.Collections.Generic;

using Netptune.Core.BaseEntities;
using Netptune.Core.Enums;

namespace Netptune.Core.Entities
{
    public class ActivityLog : WorkspaceEntity<int>
    {
        public EntityType EntityType { get; set; }

        public string UserId { get; set; }

        public ActivityType Type { get; set; }

        public int? EntityId { get; set; }

        public DateTime Time { get; set; }

        public List<int> Ancestors { get; set; }
    }
}
