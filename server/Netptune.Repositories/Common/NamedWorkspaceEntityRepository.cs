using Microsoft.EntityFrameworkCore;

using Netptune.Core.BaseEntities;
using Netptune.Core.Repositories.Common;

namespace Netptune.Repositories.Common;

public abstract class NamedWorkspaceEntityRepository<TContext, TEntity, TId>
    : WorkspaceEntityRepository<TContext, TEntity, TId>, INamedWorkspaceEntityRepository<TEntity, TId>
    where TContext : DbContext
    where TEntity : WorkspaceEntity<TId>, INamedEntity
{
    protected NamedWorkspaceEntityRepository(TContext context, IDbConnectionFactory connectionFactory)
        : base(context, connectionFactory)
    {
    }

    public Task<List<string>> GetExistingNames(
        IReadOnlyCollection<string> names,
        int workspaceId,
        CancellationToken cancellationToken = default)
    {
        if (names.Count == 0)
        {
            return Task.FromResult(new List<string>());
        }

        return Entities
            .AsNoTracking()
            .Where(entity => names.Contains(entity.Name) && entity.WorkspaceId == workspaceId && !entity.IsDeleted)
            .Select(entity => entity.Name)
            .ToListAsync(cancellationToken);
    }
}
