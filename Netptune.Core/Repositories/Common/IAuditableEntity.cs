using System;

namespace Netptune.Core.Repositories.Common
{
    public interface IAuditableEntity
    {
        DateTime DateCreated { get; set; }

        DateTime? DateModified { get; set; }

        DateTime? DateDeleted { get; set; }

        bool Deleted { get; set; }
    }
}
