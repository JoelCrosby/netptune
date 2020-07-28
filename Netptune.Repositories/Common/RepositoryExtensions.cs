using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;

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

        public static IQueryable<TEntity> IsReadonly<TEntity>
            (this IQueryable<TEntity> entities, bool isReadonly) where TEntity : class
        {
            if (isReadonly)
            {
                return entities.AsNoTracking();
            }

            return entities;
        }
    }
}
