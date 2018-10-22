using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Netptune.Models
{
    public class ProjectType : BaseModel
    {

        [Required]
        [StringLength(128)]
        public string Name { get; set; }

        [StringLength(1024)]
        public string Description { get; set; }

        [StringLength(128)]
        public string TypeCode { get; set; }

        // Navigation property 
        public virtual ICollection<Project> Projects { get; } = new List<Project>();

    }
}
