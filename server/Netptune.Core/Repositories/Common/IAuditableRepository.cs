using System.Threading.Tasks;

using Netptune.Core.BaseEntities;
using Netptune.Core.Entities;

namespace Netptune.Core.Repositories.Common;

public interface IAuditableRepository<TEntity, in TId> : IRepository<TEntity, TId>
    where TEntity : AuditableEntity<TId>
{
    Task<TEntity?> Delete(TId id, AppUser user);
}
