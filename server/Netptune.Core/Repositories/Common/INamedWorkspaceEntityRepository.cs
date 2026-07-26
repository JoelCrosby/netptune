using Netptune.Core.BaseEntities;

namespace Netptune.Core.Repositories.Common;

public interface INamedWorkspaceEntityRepository<TEntity, TId> : IWorkspaceEntityRepository<TEntity, TId>
    where TEntity : WorkspaceEntity<TId>, INamedEntity
{
    Task<List<string>> GetExistingNames(IReadOnlyCollection<string> names, int workspaceId, CancellationToken cancellationToken = default);
}
