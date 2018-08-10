using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataPlane.Models
{
    public class Project : BaseModel
    { 

        // Primary key
        public int ProjectId { get; set; }

        [Required]
        public string Name { get; set; }
        public string Description { get; set; }

        [ForeignKey("ProjectType")]
        public int? ProjectTypeId { get; set; }

        // Navigation properties
        public virtual ProjectType ProjectType { get; set; }

    }
}