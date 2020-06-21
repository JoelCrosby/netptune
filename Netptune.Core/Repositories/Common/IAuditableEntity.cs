using System;

namespace Netptune.Core.Repositories.Common
{
    public interface IAuditableEntity
    {
        bool IsDeleted { get; set; }

        byte[] Version { get; set; }

        DateTime CreatedAt { get; set; }

        DateTime? UpdatedAt { get; set; }

        #region ForeignKeys

        string CreatedByUserId { get; set; }

        string ModifiedByUserId { get; set; }

        string DeletedByUserId { get; set; }

        string OwnerId { get; set; }

        #endregion
    }
}
