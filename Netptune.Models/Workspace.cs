using System.Collections.Generic;

using Netptune.Models.BaseEntities;
using Netptune.Models.Relationships;

using Newtonsoft.Json;

namespace Netptune.Models
{
    public class Workspace : AuditableEntity<int>
    {
        public string Name { get; set; }

        public string Description { get; set; }

        #region NavigationProperties

        [JsonIgnore]
        public virtual ICollection<Project> Projects { get; set; } = new HashSet<Project>();

        [JsonIgnore]
        public virtual ICollection<WorkspaceAppUser> WorkspaceUsers { get; set; } = new HashSet<WorkspaceAppUser>();

        [JsonIgnore]
        public virtual ICollection<WorkspaceProject> WorkspaceProjects { get; set; } = new HashSet<WorkspaceProject>();

        [JsonIgnore]
        public virtual ICollection<ProjectTask> ProjectTasks { get; set; } = new HashSet<ProjectTask>();
        #endregion
    }
}
