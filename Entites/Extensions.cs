using Microsoft.EntityFrameworkCore;
using System.Linq;
using Netptune.Interfaces;

namespace Netptune.Entites
{
    public static class Extensions
    {
        public static IQueryable<T> IncludeBaseObjects<T>(this IQueryable<T> queryable) where T : class
        {
            if (queryable is IQueryable<IBaseEntity> dbSet)
            {
                return dbSet.Include(m => m.CreatedByUser)
                        .Include(m => m.ModifiedByUser)
                        .Include(m => m.Owner)
                        .Include(m => m.DeletedByUser) as IQueryable<T>;

            }

            return null;
        }
    }
}