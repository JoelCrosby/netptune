using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataPlane.Models
{
    public abstract class BaseModel
    {

        public bool IsDeleted { get; set; }

        [Timestamp]
        public byte[] Version { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTimeOffset CreatedAt { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTimeOffset UpdatedAt { get; set; }

        public int CreatedByUserId { get; set; }

        public int DeletedByUserId { get; set; }

        public int OwnerId { get; set; }


    }
}
