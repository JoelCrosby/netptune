using Netptune.Core.Relationships;
using Netptune.Core.Repositories;
using Netptune.Core.Repositories.Common;
using Netptune.Entities.Contexts;
using Netptune.Repositories.Common;
using Microsoft.EntityFrameworkCore;

namespace Netptune.Repositories;

public sealed class TaskFileRepository : Repository<DataContext, TaskFile, int>, ITaskFileRepository
{
    public TaskFileRepository(DataContext context, IDbConnectionFactory connectionFactory) : base(context, connectionFactory) { }

    public Task<TaskFile?> GetForTask(int taskId, int workspaceFileId, CancellationToken cancellationToken = default)
    {
        return Entities.SingleOrDefaultAsync(link => link.ProjectTaskId == taskId && link.WorkspaceFileId == workspaceFileId, cancellationToken);
    }

    public Task<int> CountByWorkspaceFileId(int workspaceFileId, CancellationToken cancellationToken = default)
    {
        return Entities.CountAsync(link => link.WorkspaceFileId == workspaceFileId, cancellationToken);
    }

    public Task<bool> ExistsByWorkspaceFileId(int workspaceFileId, CancellationToken cancellationToken = default)
    {
        return Entities.AnyAsync(link => link.WorkspaceFileId == workspaceFileId, cancellationToken);
    }

    public async Task DeleteByWorkspaceFileId(int workspaceFileId, CancellationToken cancellationToken = default)
    {
        await Entities
            .Where(link => link.WorkspaceFileId == workspaceFileId)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
