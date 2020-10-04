using System.Text.Json.Serialization;

using Netptune.Models.BaseEntities;

namespace Netptune.Models.Relationships
{
    public class WorkspaceAppUser : KeyedEntity<int>
    {
        public int WorkspaceId { get; set; }

        public string UserId { get; set; }

        #region NavigationProperties

        [JsonIgnore]
        public Workspace Workspace { get; set; }

        [JsonIgnore]
        public AppUser User { get; set; }

        #endregion
    }
}
