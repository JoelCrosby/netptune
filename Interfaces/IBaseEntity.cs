using System;

namespace DataPlane.Interfaces
{
    public interface IBaseEntity
    {
        bool IsDeleted { get; set; }

        byte[] Version { get; set; }

        DateTime CreatedAt { get; set; }
        DateTime UpdatedAt { get; set; }

        string CreatedByUserId { get; set; }
        string ModifiedByUserId { get; set; }
        string DeletedByUserId { get; set; }

        string OwnerId { get; set; }
    }
}
