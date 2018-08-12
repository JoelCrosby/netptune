using System.ComponentModel.DataAnnotations;

namespace DataPlane.Models
{
    public class Flag : BaseModel
    {

        // Primary key
        [Key]
        public int FlagId { get; set; }

        [Required]
        public string Name { get; set; }
        public string Description { get; set; }

    }
}
