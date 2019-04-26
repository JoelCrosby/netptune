using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Netptune.Models.Models.Relationships;
using Newtonsoft.Json;

namespace Netptune.Models.Models
{
    public class Project : BaseModel
    {

        [Required]
        [StringLength(128)]
        public string Name { get; set; }

        [StringLength(1024)]
        public string Description { get; set; }

        [StringLength(256)]
        public string RepositoryUrl { get; set; }

    #region ForeignKeys

        [Required]
        [ForeignKey("Workspace")]
        public int? WorkspaceId { get; set; }

    #endregion

    #region NavigationProperties

        [JsonIgnore]
        public virtual Workspace Workspace { get; set; }

        [JsonIgnore]
        public virtual ICollection<WorkspaceAppUser> WorkspaceUsers { get; } = new List<WorkspaceAppUser>();

        [JsonIgnore]
        public virtual ICollection<WorkspaceProject> WorkspaceProjects { get; } = new List<WorkspaceProject>();

        [JsonIgnore]
        public virtual ICollection<ProjectUser> ProjectUsers { get; } = new List<ProjectUser>();

        [JsonIgnore]
        public virtual ICollection<ProjectTask> ProjectTasks { get; } = new List<ProjectTask>();
        
        [JsonIgnore]
        public virtual ICollection<Post> ProjectPosts { get; } = new List<Post>();

    #endregion
    
    }
}
