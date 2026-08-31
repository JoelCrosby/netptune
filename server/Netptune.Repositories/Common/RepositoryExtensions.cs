using Microsoft.EntityFrameworkCore;

namespace Netptune.Repositories.Common;

public static class RepositoryExtensions
{
    public static Task<List<TEntity>> ToReadonlyListAsync<TEntity>
        (this IQueryable<TEntity> query, bool isReadonly, CancellationToken cancellationToken = default) where TEntity : class
    {
        if (isReadonly)
        {
            return query.AsNoTracking().ToListAsync(cancellationToken);
        }

        return query.ToListAsync(cancellationToken);
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
