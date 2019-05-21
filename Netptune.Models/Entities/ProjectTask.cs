using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Netptune.Entities.Entites.BaseEntities;
using Netptune.Entities.Enums;
using Newtonsoft.Json;

namespace Netptune.Entities.Entites 
{
    public class ProjectTask : AuditableEntity<int>
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

    #region NavigationProperties

        [JsonIgnore]
        public virtual AppUser Assignee { get; set; }

        [JsonIgnore]
        public virtual Project Project { get; set; }

        [JsonIgnore]
        public virtual Workspace Workspace { get; set; }

    #endregion
    
    }
}
