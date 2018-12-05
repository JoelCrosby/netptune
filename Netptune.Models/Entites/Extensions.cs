using System.Linq;
using Microsoft.EntityFrameworkCore;
using Netptune.Models.Interfaces;

namespace Netptune.Models.Entites
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