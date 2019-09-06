using Netptune.Models.BaseEntities;
using Netptune.Models.Enums;

using Newtonsoft.Json;

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
        public virtual AppUser Assignee { get; set; }

        [JsonIgnore]
        public virtual Project Project { get; set; }

        [JsonIgnore]
        public virtual Workspace Workspace { get; set; }

        #endregion

    }
}
