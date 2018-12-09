using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Netptune.Models.Enums;

namespace Netptune.Models.Models
{
    public class Post : BaseModel
    {

        [Required]
        [StringLength(128)]
        public string Title { get; set; }

        [StringLength(4096)]
        public string Body { get; set; }

        public PostType Type { get; set; }

        [Required]
        [ForeignKey ("Project")]
        public int ProjectId { get; set; }

        // Navigation properties
        public virtual Project Project { get; set; }
    }
}