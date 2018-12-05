using Netptune.Models.Relationships;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Netptune.Models
{
    public class Workspace : BaseModel
    {

        [Required]
        [StringLength(128)]
        public string Name { get; set; }

        [StringLength(1024)]
        public string Description { get; set; }

        // Navigation properties
        public virtual ICollection<Project> Projects { get; set; }

        public virtual ICollection<WorkspaceAppUser> WorkspaceUsers { get; } = new List<WorkspaceAppUser>();
        public virtual ICollection<WorkspaceProject> WorkspaceProjects { get; } = new List<WorkspaceProject>();
        public virtual ICollection<ProjectTask> ProjectTasks { get; } = new List<ProjectTask>();
    }
}
