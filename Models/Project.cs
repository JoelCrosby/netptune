using DataPlane.Models.Relationships;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataPlane.Models
{
    public class Project : BaseModel
    {

        // Primary key
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ProjectId { get; set; }

        [Required]
        public string Name { get; set; }
        public string Description { get; set; }

        [ForeignKey("ProjectType")]
        public int? ProjectTypeId { get; set; }

        [ForeignKey("Workspace")]
        public int? WorkspaceId { get; set; }

        // Navigation properties
        public virtual ProjectType ProjectType { get; set; }
        public virtual Workspace Workspace { get; set; }

        public virtual ICollection<WorkspaceAppUser> WorkspaceUsers { get; set; }
        public virtual ICollection<WorkspaceProject> WorkspaceProjects { get; set; }

        public virtual ICollection<ProjectUser> ProjectUsers { get; set; }
    }
}