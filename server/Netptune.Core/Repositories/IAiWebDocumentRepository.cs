using Netptune.Core.Entities;
using Netptune.Core.Repositories.Common;

namespace Netptune.Core.Repositories;

public interface IAiWebDocumentRepository : IRepository<AiWebDocument, Guid>
{
    Task<AiWebDocument?> GetInWorkspace(Guid id, int workspaceId, CancellationToken cancellationToken = default);

    Task<int> DeleteExpired(DateTime cutoff, CancellationToken cancellationToken = default);
}
