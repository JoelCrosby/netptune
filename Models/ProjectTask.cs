using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using DataPlane.Enums;
using DataPlane.Interfaces;

namespace DataPlane.Models 
{
    public class ProjectTask : BaseModel , IBaseEntity
    {

        // Primary key
        [Key]
        public int ProjectTaskId { get; set; }

        [Required]
        public string Name { get; set; }
        public string Description { get; set; }
        public ProjectTaskStatus Status { get; set; }

        // Has unique index.
        public double SortOrder { get; set; }

#region ForeignKeys

        [ForeignKey ("Assignee")]
        public string AssigneeId { get; set; }

        [ForeignKey ("Project")]
        public int? ProjectId { get; set; }

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