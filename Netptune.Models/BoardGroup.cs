using System.Text.Json.Serialization;

using Netptune.Models.BaseEntities;

namespace Netptune.Models
{
    public class BoardGroup : AuditableEntity<int>
    {
        public string Name { get; set; }

        public int BoardId { get; set; }

        #region NavigationProperties

        [JsonIgnore]
        public Board Board { get; set; }

        #endregion
    }
}