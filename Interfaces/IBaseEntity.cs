using System;
using DataPlane.Models;

namespace DataPlane.Interfaces
{
    public interface IBaseEntity
    {
        bool IsDeleted { get; set; }

        byte[] Version { get; set; }

        DateTime CreatedAt { get; set; }
        DateTime UpdatedAt { get; set; }

        string CreatedByUserId { get; set; }
        AppUser CreatedByUser { get; set; }

        string ModifiedByUserId { get; set; }
        AppUser ModifiedByUser { get; set; }

        string DeletedByUserId { get; set; }
        AppUser DeletedByUser { get; set; }

        string OwnerId { get; set; }
        AppUser Owner { get; set; }
    }
}
