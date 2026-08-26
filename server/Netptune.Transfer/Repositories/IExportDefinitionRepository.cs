using Netptune.Core.Repositories.Common;
using Netptune.Transfer.Entities;

namespace Netptune.Transfer.Repositories;

public interface IExportDefinitionRepository : IWorkspaceEntityRepository<ExportDefinition, int>
{
    Task<List<ExportDefinition>> GetVisibleInWorkspace(int workspaceId, string currentUserId, CancellationToken cancellationToken = default);

    Task<bool> NameExists(int workspaceId, string name, int? excludeId, CancellationToken cancellationToken = default);
}
