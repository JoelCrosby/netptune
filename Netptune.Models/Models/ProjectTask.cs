using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Netptune.Models.Enums;

namespace Netptune.Models.Models 
{
    public class ProjectTask : BaseModel
    {

        [Required]
        [StringLength(128)]
        public string Name { get; set; }

        [StringLength(1024)]
        public string Description { get; set; }

        public ProjectTaskStatus Status { get; set; }
        public double SortOrder { get; set; }

#region ForeignKeys

        [ForeignKey ("Assignee")]
        public string AssigneeId { get; set; }

        [Required]
        [ForeignKey ("Project")]
        public int? ProjectId { get; set; }

        [Required]
        [ForeignKey ("Workspace")]
        public int? WorkspaceId { get; set; }

#endregion

#region ForeignKeys

        public virtual AppUser Assignee { get; set; }
        public virtual Project Project { get; set; }
        public virtual Workspace Workspace { get; set; }

#endregion
    }
}
