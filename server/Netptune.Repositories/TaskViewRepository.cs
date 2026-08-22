using Microsoft.EntityFrameworkCore;

using Netptune.Core.Entities;
using Netptune.Core.Repositories;
using Netptune.Core.Repositories.Common;
using Netptune.Entities.Contexts;
using Netptune.Repositories.Common;

namespace Netptune.Repositories;

public sealed class TaskViewRepository : WorkspaceEntityRepository<DataContext, TaskView, int>, ITaskViewRepository
{
    public TaskViewRepository(DataContext context, IDbConnectionFactory connectionFactory) : base(context, connectionFactory) { }

    public Task<List<TaskView>> GetVisibleInWorkspace(int workspaceId, string currentUserId, CancellationToken cancellationToken = default)
    {
        return Entities
            .AsNoTracking()
            .Include(view => view.CreatedByUser)
            .Where(view => view.WorkspaceId == workspaceId && !view.IsDeleted)
            .Where(view => view.IsShared || view.CreatedByUserId == currentUserId)
            .OrderBy(view => view.Name)
            .ToListAsync(cancellationToken);
    }

    public override Task<TaskView?> GetInWorkspace(int id, int workspaceId, bool isReadonly = false, CancellationToken cancellationToken = default)
    {
        var query = Entities
            .Include(view => view.CreatedByUser)
            .Where(view => view.Id == id && view.WorkspaceId == workspaceId && !view.IsDeleted);

        if (isReadonly)
        {
            return query.AsNoTracking().SingleOrDefaultAsync(cancellationToken);
        }

        return query.SingleOrDefaultAsync(cancellationToken);
    }

    public Task<bool> NameExists(int workspaceId, string name, int? excludeId, CancellationToken cancellationToken = default)
    {
        return Entities.AnyAsync(
            view => view.WorkspaceId == workspaceId &&
                !view.IsDeleted &&
                view.Name.ToLower() == name.ToLower() &&
                (excludeId == null || view.Id != excludeId),
            cancellationToken);
    }

    public Task<TaskView?> GetBySlug(string slug, int workspaceId, bool isReadonly = false, CancellationToken cancellationToken = default)
    {
        var query = Entities
            .Include(view => view.CreatedByUser)
            .Where(view => view.Slug == slug && view.WorkspaceId == workspaceId && !view.IsDeleted);

        if (isReadonly)
        {
            return query.AsNoTracking().SingleOrDefaultAsync(cancellationToken);
        }

        return query.SingleOrDefaultAsync(cancellationToken);
    }
}
