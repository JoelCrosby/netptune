using Netptune.Core.Entities;
using Netptune.Core.Repositories.Common;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.Files;

namespace Netptune.Core.Repositories;

public interface IWorkspaceFileRepository : IWorkspaceEntityRepository<WorkspaceFile, int>
{
    Task<PagedResponse<WorkspaceFileViewModel>> GetWorkspaceFiles(int workspaceId, string currentUserId, bool canDeleteAny, WorkspaceFileFilter filter, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WorkspaceFileViewModel>> GetTaskFiles(int workspaceId, int taskId, string currentUserId, bool canDeleteAny, CancellationToken cancellationToken = default);

    Task<WorkspaceFile?> GetInWorkspace(int id, int workspaceId, bool isReadonly = false, CancellationToken cancellationToken = default);

    Task<WorkspaceFile?> GetByContentId(string contentId, int workspaceId, bool isReadonly = false, CancellationToken cancellationToken = default);

    Task<WorkspaceFileViewModel?> GetViewModel(int id, string currentUserId, bool canDeleteAny, CancellationToken cancellationToken = default);

    Task<bool> TryMarkQuotaReleased(int id, string userId, CancellationToken cancellationToken = default);

    Task<List<WorkspaceFile>> GetStalePending(DateTime createdBefore, CancellationToken cancellationToken = default);

    Task MarkReady(int id, CancellationToken cancellationToken = default);

    Task<List<string>> GetTombstoneStorageKeys(CancellationToken cancellationToken = default);

    Task<long> GetExpectedStorageUsage(int workspaceId, CancellationToken cancellationToken = default);
}
