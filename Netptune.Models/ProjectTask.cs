using System.Text.Json.Serialization;

using Netptune.Models.BaseEntities;
using Netptune.Models.Enums;

namespace Netptune.Models
{
    public class ProjectTask : AuditableEntity<int>
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public ProjectTaskStatus Status { get; set; }

        public double SortOrder { get; set; }

        #region ForeignKeys

        public string AssigneeId { get; set; }

        public int? ProjectId { get; set; }

        public int? WorkspaceId { get; set; }

        #endregion

        #region NavigationProperties

        [JsonIgnore]
        public AppUser Assignee { get; set; }

        [JsonIgnore]
        public Project Project { get; set; }

        [JsonIgnore]
        public Workspace Workspace { get; set; }

        #endregion

    }
}
