using System.Collections.Generic;
using System.Text.Json.Serialization;

using Netptune.Core.BaseEntities;
using Netptune.Core.Enums;

namespace Netptune.Core.Entities
{
    public class Comment : AuditableEntity<int>
    {
        public string Body { get; set; }

        public int EntityId { get; set; }

        public EntityType EntityType { get; set; }

        #region NavigationProperties

        [JsonIgnore]
        public ICollection<Reaction> Reactions { get; set; } = new HashSet<Reaction>();

        #endregion
    }
}
