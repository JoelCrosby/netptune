using Netptune.Interfaces;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Netptune.Models
{
    public abstract class BaseModel : IBaseEntity
    {

        // Primary key
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public bool IsDeleted { get; set; }

        [Timestamp]
        public byte[] Version { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        [ForeignKey("CreatedByUser")]
        public string CreatedByUserId { get; set; }
        public virtual AppUser CreatedByUser { get; set; }

        [ForeignKey("ModifiedByUser")]
        public string ModifiedByUserId { get; set; }
        public virtual AppUser ModifiedByUser { get; set; }

        [ForeignKey("DeletedByUser")]
        public string DeletedByUserId { get; set; }
        public virtual AppUser DeletedByUser { get; set; }

        [ForeignKey("Owner")]
        public string OwnerId { get; set; }
        public virtual AppUser Owner { get; set; }

    }
}
