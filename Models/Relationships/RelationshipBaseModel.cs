using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataPlane.Models.Relationships
{
    public abstract class RelationshipBaseModel
    {
        // Primary key
        [Key]
        public int Id { get; set; }
    }
}
