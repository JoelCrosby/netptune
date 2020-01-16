using System.Collections.Generic;
using System.Text.Json.Serialization;

using Netptune.Models.BaseEntities;
using Netptune.Models.Meta;
using Netptune.Models.Relationships;

namespace Netptune.Models
{
    public class Workspace : AuditableEntity<int>
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public string Slug { get; set; }

        public WorkspaceMeta MetaInfo { get; set; }

        #region NavigationProperties

        [JsonIgnore]
        public ICollection<Project> Projects { get; set; } = new HashSet<Project>();

        [JsonIgnore]
        public ICollection<WorkspaceAppUser> WorkspaceUsers { get; set; } = new HashSet<WorkspaceAppUser>();

        [JsonIgnore]
        public ICollection<WorkspaceProject> WorkspaceProjects { get; set; } = new HashSet<WorkspaceProject>();

        [JsonIgnore]
        public ICollection<ProjectTask> ProjectTasks { get; set; } = new HashSet<ProjectTask>();

        #endregion
    }
}
