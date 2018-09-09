using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DataPlane.Interfaces;

namespace DataPlane.Models
{
    public class ProjectType : BaseModel, IBaseEntity
    {
        // Primary key
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }
        public string Description { get; set; }
        public string TypeCode { get; set; }

        // Navigation property 
        public virtual ICollection<Project> Projects { get; } = new List<Project>();

    }
}
