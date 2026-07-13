using Netptune.Core.Entities;
using Netptune.Core.Repositories.Common;
using Netptune.Core.ViewModels.RelationTypes;

namespace Netptune.Core.Repositories;

public interface IRelationTypeRepository : IWorkspaceEntityRepository<RelationType, int>
{
    Task<List<RelationTypeViewModel>> GetViewModelsForWorkspace(int workspaceId, CancellationToken cancellationToken = default);

    Task<RelationTypeViewModel?> GetViewModel(int id, CancellationToken cancellationToken = default);

    Task<RelationType?> GetInWorkspace(int id, int workspaceId, bool isReadonly = false, CancellationToken cancellationToken = default);

    Task<bool> KeyExists(int workspaceId, string key, int? excludingId = null, CancellationToken cancellationToken = default);

    Task<bool> IsInUse(int relationTypeId, CancellationToken cancellationToken = default);

    Task EnsureDefaultRelationTypes(int workspaceId, string? ownerId, CancellationToken cancellationToken = default);
}
