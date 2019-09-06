using System.Collections.Generic;

using Netptune.Models.BaseEntities;
using Netptune.Models.Relationships;

using Newtonsoft.Json;

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
        public virtual ICollection<WorkspaceAppUser> WorkspaceUsers { get; set; }

        [JsonIgnore]
        public virtual ICollection<WorkspaceProject> WorkspaceProjects { get; set; }

        [JsonIgnore]
        public virtual ICollection<ProjectUser> ProjectUsers { get; set; }

        [JsonIgnore]
        public virtual ICollection<ProjectTask> ProjectTasks { get; set; }

        [JsonIgnore]
        public virtual ICollection<Post> ProjectPosts { get; set; }

        #endregion

    }
}
