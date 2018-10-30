using Netptune.Models.Relationships;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Netptune.Models
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

        [Required]
        [ForeignKey("ProjectType")]
        public int? ProjectTypeId { get; set; }

        [Required]
        [ForeignKey("Workspace")]
        public int? WorkspaceId { get; set; }

        // Navigation properties
        public virtual ProjectType ProjectType { get; set; }
        public virtual Workspace Workspace { get; set; }

        public virtual ICollection<WorkspaceAppUser> WorkspaceUsers { get; } = new List<WorkspaceAppUser>();
        public virtual ICollection<WorkspaceProject> WorkspaceProjects { get; } = new List<WorkspaceProject>();

        public virtual ICollection<ProjectUser> ProjectUsers { get; } = new List<ProjectUser>();
        public virtual ICollection<ProjectTask> ProjectTasks { get; } = new List<ProjectTask>();
        public virtual ICollection<Post> ProjectPosts { get; } = new List<Post>();
    }
}
