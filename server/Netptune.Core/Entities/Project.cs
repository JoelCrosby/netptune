using System.Collections.Generic;
using System.Text.Json.Serialization;

using Netptune.Core.BaseEntities;
using Netptune.Core.Relationships;

namespace Netptune.Core.Entities
{
    public class Project : WorkspaceEntity<int>
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public string RepositoryUrl { get; set; }

        public string Key { get; set; }

        #region NavigationProperties

        [JsonIgnore]
        public ICollection<WorkspaceProject> WorkspaceProjects { get; set; } = new HashSet<WorkspaceProject>();

        [JsonIgnore]
        public ICollection<ProjectUser> ProjectUsers { get; set; } = new HashSet<ProjectUser>();

        [JsonIgnore]
        public ICollection<ProjectTask> ProjectTasks { get; set; } = new HashSet<ProjectTask>();

        [JsonIgnore]
        public ICollection<Post> ProjectPosts { get; set; } = new HashSet<Post>();

        [JsonIgnore]
        public ICollection<Board> ProjectBoards { get; set; } = new HashSet<Board>();

        #endregion
    }
}
