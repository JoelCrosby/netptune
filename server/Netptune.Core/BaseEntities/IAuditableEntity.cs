using System;

using Netptune.Core.Entities;

namespace Netptune.Core.BaseEntities;

public interface IAuditableEntity
{
    bool IsDeleted { get; set; }

    DateTime CreatedAt { get; set; }

    DateTime? UpdatedAt { get; set; }

    string CreatedByUserId { get; set; }

    string ModifiedByUserId { get; set; }

    string DeletedByUserId { get; set; }

    string OwnerId { get; set; }

    AppUser CreatedByUser { get; set; }

    AppUser ModifiedByUser { get; set; }

    AppUser DeletedByUser { get; set; }

    AppUser Owner { get; set; }
}

public interface IAuditableEntity<TValue> : IKeyedEntity<TValue>, IAuditableEntity
{
}
