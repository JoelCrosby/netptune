using DataPlane.Interfaces;
using DataPlane.Models.Relationships;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace DataPlane.Models
{
    public class Workspace : BaseModel, IBaseEntity
    {

        // Primary key
        [Key]
        public int WorkspaceId { get; set; }

        [Required]
        public string Name { get; set; }
        public string Description { get; set; }

        // Navigation properties
        public virtual ICollection<Project> Projects { get; set; }

        public virtual ICollection<WorkspaceAppUser> WorkspaceUsers { get; } = new List<WorkspaceAppUser>();
        public virtual ICollection<WorkspaceProject> WorkspaceProjects { get; } = new List<WorkspaceProject>();
        public virtual ICollection<ProjectTask> ProjectTasks { get; } = new List<ProjectTask>();
    }
}
