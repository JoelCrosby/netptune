using System;

using Netptune.Core.Entities;

namespace Netptune.Core.BaseEntities
{
    public interface IAuditableEntity<TValue> : IKeyedEntity<TValue>
    {
        bool IsDeleted { get; set; }

        DateTime CreatedAt { get; set; }

        DateTime? UpdatedAt { get; set; }

        #region ForeignKeys

        string CreatedByUserId { get; set; }

        string ModifiedByUserId { get; set; }

        string DeletedByUserId { get; set; }

        string OwnerId { get; set; }

        #endregion

        #region NavigationProperties

        AppUser CreatedByUser { get; set; }

        AppUser ModifiedByUser { get; set; }

        AppUser DeletedByUser { get; set; }

        AppUser Owner { get; set; }

        #endregion
    }
}
