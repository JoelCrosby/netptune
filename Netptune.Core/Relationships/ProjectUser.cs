using Netptune.Core.BaseEntities;
using Netptune.Core.Entities;

using System.Text.Json.Serialization;

namespace Netptune.Core.Relationships
{
    public class ProjectUser : KeyedEntity<int>
    {
        public int ProjectId { get; set; }

        public string UserId { get; set; }

        #region NavigationProperties

        [JsonIgnore]
        public Project Project { get; set; }

        [JsonIgnore]
        public AppUser User { get; set; }

        #endregion
    }
}
