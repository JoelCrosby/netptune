using DataPlane.Interfaces;
using System;
using System.ComponentModel.DataAnnotations;

namespace DataPlane.Models
{
    public abstract class BaseModel : IBaseEntity
    {

        public bool IsDeleted { get; set; }

        [Timestamp]
        public byte[] Version { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public string CreatedByUserId { get; set; }
        public string ModifiedByUserId { get; set; }
        public string DeletedByUserId { get; set; }

        public string OwnerId { get; set; }


    }
}
