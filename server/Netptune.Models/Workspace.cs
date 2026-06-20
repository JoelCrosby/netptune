using System.Text.Json.Serialization;

using Netptune.Models.BaseEntities;
using Netptune.Models.Meta;
using Netptune.Models.Relationships;

namespace Netptune.Models
{
    public class Workspace : AuditableEntity<int>
    {
        public required string Name { get; set; }

        public required string Description { get; set; }

        public required string Slug { get; set; }

        public required WorkspaceMeta MetaInfo { get; set; }

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
