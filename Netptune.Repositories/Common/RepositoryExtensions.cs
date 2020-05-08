using Microsoft.EntityFrameworkCore;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Netptune.Repositories.Common
{
    public static class RepositoryExtensions
    {
        public static Task<List<TEntity>> ApplyReadonly<TEntity>
            (this IQueryable<TEntity> entities, bool isReadonly) where TEntity : class
        {
            if (isReadonly)
            {
                return entities.AsNoTracking().ToListAsync();
            }

            return entities.ToListAsync();
        }
    }
}
