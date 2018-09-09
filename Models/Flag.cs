using System.ComponentModel.DataAnnotations;
using DataPlane.Interfaces;

namespace DataPlane.Models
{
    public class Flag : BaseModel, IBaseEntity
    {

        // Primary key
        [Key]
        public int FlagId { get; set; }

        [Required]
        public string Name { get; set; }
        public string Description { get; set; }

    }
}
