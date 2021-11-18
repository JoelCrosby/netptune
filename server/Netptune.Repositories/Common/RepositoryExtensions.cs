using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;

namespace Netptune.Repositories.Common;

public static class RepositoryExtensions
{
    public static Task<List<TEntity>> ToReadonlyListAsync<TEntity>
        (this IQueryable<TEntity> query, bool isReadonly) where TEntity : class
    {
        if (isReadonly)
        {
            return query.AsNoTracking().ToListAsync();
        }

        return query.ToListAsync();
    }

    public static List<TEntity> ToReadonlyList<TEntity>
        (this IQueryable<TEntity> query, bool isReadonly) where TEntity : class
    {
        if (isReadonly)
        {
            return query.AsNoTracking().ToList();
        }

        return query.ToList();
    }

    public static IQueryable<TEntity> IsReadonly<TEntity>
        (this IQueryable<TEntity> query, bool isReadonly) where TEntity : class
    {
        if (isReadonly)
        {
            return query.AsNoTracking();
        }

        return query;
    }
}