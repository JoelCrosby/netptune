using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace DataPlane.Models
{
    public class Password
    {
        // Primary key
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [ForeignKey("Owner")]
        public int UserId { get; set; }

        [Required]
        public string Hash { get; set; }

        [Required]
        public string Salt { get; set; }

        // Navigation property 
        public virtual User Owner { get; set; }
    }
}
