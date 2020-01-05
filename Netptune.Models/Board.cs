using System.Collections.Generic;
using System.Text.Json.Serialization;

using Netptune.Models.BaseEntities;

namespace Netptune.Models
{
    public class Board : AuditableEntity<int>
    {
        public string Name { get; set; }

        public string Identifier { get; set; }

        public int ProjectId { get; set; }

        #region NavigationProperties

        [JsonIgnore]
        public ICollection<BoardGroup> BoardGroups { get; set; } = new HashSet<BoardGroup>();

        [JsonIgnore]
        public Project Project { get; set; }

        #endregion

    }
}
