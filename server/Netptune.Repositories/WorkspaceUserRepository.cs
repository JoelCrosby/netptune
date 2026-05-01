using Microsoft.EntityFrameworkCore;

using Netptune.Core.Models;
using Netptune.Core.Relationships;
using Netptune.Core.Repositories;
using Netptune.Core.Repositories.Common;
using Netptune.Entities.Contexts;
using Netptune.Repositories.Common;

namespace Netptune.Repositories;

public class WorkspaceUserRepository : Repository<DataContext, WorkspaceAppUser, int>, IWorkspaceUserRepository
{
    public WorkspaceUserRepository(DataContext context, IDbConnectionFactory connectionFactory)
        : base(context, connectionFactory)
    {
    }

    public async Task<UserPermissions?> GetUserPermissions(string userId, string workspaceKey, bool isReadOnly = true, CancellationToken cancellationToken = default)
    {
        var workspaceUser = await Context.WorkspaceAppUsers
            .Where(p => p.UserId == userId && p.Workspace.Slug == workspaceKey)
            .Select(p => new { p.Role, p.Permissions })
            .IsReadonly(isReadOnly)
            .SingleOrDefaultAsync(cancellationToken);

        if (workspaceUser is null) return null;

        return new UserPermissions
        {
            UserId = userId,
            WorkspaceKey = workspaceKey,
            Permissions = workspaceUser.Permissions.ToHashSet(),
            Role = workspaceUser.Role,
        };
    }

    public Task<List<string>> GetWorkspaceUserIds(int workspaceId, CancellationToken cancellationToken = default)
    {
        return Context.WorkspaceAppUsers
            .AsNoTracking()
            .Where(u => u.WorkspaceId == workspaceId)
            .Select(u => u.UserId)
            .ToListAsync(cancellationToken);
    }

    public async Task<Dictionary<int, List<string>>> GetWorkspaceUserIdsByWorkspaceIds(IEnumerable<int> workspaceIds, CancellationToken cancellationToken = default)
    {
        var rows = await Context.WorkspaceAppUsers
            .AsNoTracking()
            .Where(u => workspaceIds.Contains(u.WorkspaceId))
            .Select(u => new { u.WorkspaceId, u.UserId })
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(u => u.WorkspaceId)
            .ToDictionary(g => g.Key, g => g.Select(u => u.UserId).ToList());
    }

    public async Task SetUserPermissions(string userId, int workspaceId, IEnumerable<string> permissions, CancellationToken cancellationToken = default)
    {
        var existing = await Context.WorkspaceAppUsers
            .Where(p => p.UserId == userId && p.WorkspaceId == workspaceId)
            .FirstOrDefaultAsync(cancellationToken);

        existing?.Permissions = permissions.ToList();
    }
}
