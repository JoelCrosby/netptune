using Netptune.Core.Relationships;
using Netptune.Core.Repositories.Common;

namespace Netptune.Core.Repositories;

public interface ITaskFileRepository : IRepository<TaskFile, int>
{
    Task<TaskFile?> GetForTask(int taskId, int workspaceFileId, CancellationToken cancellationToken = default);

    Task<int> CountByWorkspaceFileId(int workspaceFileId, CancellationToken cancellationToken = default);

    Task<bool> ExistsByWorkspaceFileId(int workspaceFileId, CancellationToken cancellationToken = default);

    Task DeleteByWorkspaceFileId(int workspaceFileId, CancellationToken cancellationToken = default);
}
