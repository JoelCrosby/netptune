using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Netptune.Entities.Entites.BaseEntities;
using Netptune.Entities.Entites.Relationships;
using Newtonsoft.Json;

namespace Netptune.Entities.Entites
{
    public class Workspace : AuditableEntity<int>
    {
        [Required]
        [StringLength(128)]
        public string Name { get; set; }

        [StringLength(1024)]
        public string Description { get; set; }

    #region NavigationProperties

        [JsonIgnore]
        public virtual ICollection<Project> Projects { get; set; }

        [JsonIgnore]
        public virtual ICollection<WorkspaceAppUser> WorkspaceUsers { get; } = new List<WorkspaceAppUser>();
        
        [JsonIgnore]
        public virtual ICollection<WorkspaceProject> WorkspaceProjects { get; } = new List<WorkspaceProject>();
        
        [JsonIgnore]
        public virtual ICollection<ProjectTask> ProjectTasks { get; } = new List<ProjectTask>();

    #endregion
    
    }
}
