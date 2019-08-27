using System.Collections.Generic;
using Netptune.Entities.Entites.BaseEntities;
using Netptune.Entities.Entites.Relationships;
using Newtonsoft.Json;

namespace Netptune.Entities.Entites
{
    public class Workspace : AuditableEntity<int>
    {
        public string Name { get; set; }

        public string Description { get; set; }

        #region NavigationProperties

        [JsonIgnore]
        public virtual ICollection<Project> Projects { get; set; }

        [JsonIgnore]
        public virtual ICollection<WorkspaceAppUser> WorkspaceUsers { get; set; }

        [JsonIgnore]
        public virtual ICollection<WorkspaceProject> WorkspaceProjects { get; set; }

        [JsonIgnore]
        public virtual ICollection<ProjectTask> ProjectTasks { get; set; }
        #endregion
    }
}
