using System;
using System.Text.Json;
using System.Text.Json.Serialization;

using Netptune.Core.BaseEntities;
using Netptune.Core.Enums;

namespace Netptune.Core.Entities
{
    public class ActivityLog : WorkspaceEntity<int>, IDisposable
    {
        public EntityType EntityType { get; set; }

        public string UserId { get; set; }

        public ActivityType Type { get; set; }

        public int? EntityId { get; set; }

        public DateTime Time { get; set; }

        public int? ProjectId { get; set; }

        public int? BoardId { get; set; }

        public int? BoardGroupId { get; set; }

        public int? TaskId { get; set; }

        public JsonDocument Meta { get; set; }

        #region NavigationProperties

        [JsonIgnore]
        public AppUser User { get; set; }

        #endregion

        public void Dispose() => Meta?.Dispose();
    }
}
