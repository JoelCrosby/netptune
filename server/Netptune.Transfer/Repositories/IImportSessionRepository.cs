using Netptune.Core.Repositories.Common;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Transfer.Entities;
using Netptune.Transfer.ViewModels;

namespace Netptune.Transfer.Repositories;

public interface IImportSessionRepository : IWorkspaceEntityRepository<ImportSession, int>
{
    Task<ImportSession?> GetByPublicId(Guid publicId, int workspaceId, bool isReadonly = false, CancellationToken cancellationToken = default);

    Task<ImportSession?> GetForProcessing(int id, CancellationToken cancellationToken = default);

    Task<PagedResponse<ImportSessionViewModel>> GetSessions(int workspaceId, PageRequest page, CancellationToken cancellationToken = default);

    Task<ImportSessionViewModel?> GetViewModel(Guid publicId, int workspaceId, CancellationToken cancellationToken = default);

    Task<List<ImportSessionEntry>> GetEntries(int sessionId, CancellationToken cancellationToken = default);

    Task AddEntries(IEnumerable<ImportSessionEntry> entries, CancellationToken cancellationToken = default);

    Task<List<ImportSession>> GetExpired(DateTime expiredBefore, int take, CancellationToken cancellationToken = default);
}
