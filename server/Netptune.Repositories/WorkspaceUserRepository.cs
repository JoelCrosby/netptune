using Microsoft.EntityFrameworkCore;

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

    public async Task<HashSet<string>?> GetUserPermissions(string userId, string workspaceKey)
    {
        var isMember = await Context.WorkspaceAppUsers
            .AnyAsync(x => x.UserId == userId && x.Workspace.Slug == workspaceKey);

        if (!isMember)
        {
            return null;
        }

        var permissions = await Context.WorkspaceAppUsers
            .Where(p => p.UserId == userId && p.Workspace.Slug == workspaceKey)
            .Select(p => p.Permissions)
            .SingleOrDefaultAsync();

        return permissions?.ToHashSet() ?? [];
    }

    public async Task SetUserPermissions(string userId, int workspaceId, IEnumerable<string> permissions)
    {
        var existing = await Context.WorkspaceAppUsers
            .Where(p => p.UserId == userId && p.WorkspaceId == workspaceId)
            .FirstOrDefaultAsync();

        existing?.Permissions = permissions.ToList();
    }
}
