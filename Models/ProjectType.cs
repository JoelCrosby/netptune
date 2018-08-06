using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace DataPlane.Models
{
    public class ProjectType
    {
        // Primary key
        [Key]
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity)]
        public int ProjectTypeId { get; set; }

        [Required]
        public string Name { get; set; }
        public string Description { get; set; }
        public string TypeCode { get; set; }

        // Navigation property 
        public virtual ICollection<Project> Projects { get; set; }
    }
}
