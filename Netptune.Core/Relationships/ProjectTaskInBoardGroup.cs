using Netptune.Core.BaseEntities;

using System.Text.Json.Serialization;

namespace Netptune.Core.Relationships
{
    public class ProjectTaskInBoardGroup : AuditableEntity<int>
    {
        public int ProjectTaskId { get; set; }

        public int BoardGroupId { get; set; }

        public double SortOrder { get; set; }

        #region NavigationProperties

        [JsonIgnore]
        public ProjectTask ProjectTask { get; set; }

        [JsonIgnore]
        public BoardGroup BoardGroup { get; set; }

        #endregion
    }
}
