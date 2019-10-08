using System.Collections.Generic;
using System.Text.Json.Serialization;

using Netptune.Models.BaseEntities;
using Netptune.Models.Relationships;

namespace Netptune.Models
{
    public class Project : AuditableEntity<int>
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public string RepositoryUrl { get; set; }

        #region ForeignKeys

        public int WorkspaceId { get; set; }

        #endregion

        #region NavigationProperties

        [JsonIgnore]
        public virtual Workspace Workspace { get; set; }

        [JsonIgnore]
        public ICollection<WorkspaceAppUser> WorkspaceUsers { get; set; } = new HashSet<WorkspaceAppUser>();

        [JsonIgnore]
        public ICollection<WorkspaceProject> WorkspaceProjects { get; set; } = new HashSet<WorkspaceProject>();

        [JsonIgnore]
        public ICollection<ProjectUser> ProjectUsers { get; set; } = new HashSet<ProjectUser>();

        [JsonIgnore]
        public ICollection<ProjectTask> ProjectTasks { get; set; } = new HashSet<ProjectTask>();

        [JsonIgnore]
        public ICollection<Post> ProjectPosts { get; set; } = new HashSet<Post>();

        #endregion

    }
}
