using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace DataPlane.Models {
    public class ProjectTask : BaseModel {

        // Primary key
        [Key]
        public int ProjectTaskId { get; set; }

        [Required]
        public string Name { get; set; }
        public string Description { get; set; }

        public virtual AppUser Assignee { get; set; }

    }
}