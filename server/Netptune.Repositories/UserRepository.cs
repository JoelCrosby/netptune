using Microsoft.EntityFrameworkCore;

using Netptune.Core.Authorization;
using Netptune.Core.Entities;
using Netptune.Core.Extensions;
using Netptune.Core.Models;
using Netptune.Core.Relationships;
using Netptune.Core.Repositories;
using Netptune.Core.Repositories.Common;
using Netptune.Entities.Contexts;
using Netptune.Repositories.Common;


namespace Netptune.Repositories;

public class UserRepository : Repository<DataContext, AppUser, string>, IUserRepository
{
    public UserRepository(DataContext context, IDbConnectionFactory connectionFactory)
        : base(context, connectionFactory)
    {
    }

    public Task<List<AppUser>> GetWorkspaceUsers(string workspaceKey, bool isReadonly = false, CancellationToken cancellationToken = default)
    {
        return Context.WorkspaceAppUsers
            .Where(x => x.Workspace.Slug == workspaceKey)
            .Select(x => x.User)
            .IsReadonly(isReadonly)
            .ToListAsync(cancellationToken);
    }

    public Task<List<WorkspaceAppUser>> GetWorkspaceAppUsers(string workspaceKey, bool isReadonly = false, CancellationToken cancellationToken = default)
    {
        return Context.WorkspaceAppUsers
            .Include(x => x.User)
            .Where(x => x.Workspace.Slug == workspaceKey)
            .IsReadonly(isReadonly)
            .ToListAsync(cancellationToken);
    }

    public Task<WorkspaceRole?> GetUserWorkspaceRole(string userId, string workspaceKey, CancellationToken cancellationToken = default)
    {
        return Context.WorkspaceAppUsers
            .Where(x => x.UserId == userId && x.Workspace.Slug == workspaceKey)
            .Select(x => (WorkspaceRole?)x.Role)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<WorkspaceAppUser> InviteUserToWorkspace(string userId, int workspaceId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(userId)) throw new ArgumentNullException(nameof(userId));

        var invite = new WorkspaceAppUser
        {
            WorkspaceId = workspaceId,
            UserId = userId,
            Role = WorkspaceRole.Member,
        };

        var result = await Context.WorkspaceAppUsers.AddAsync(invite, cancellationToken);

        await SeedDefaultPermissionsAsync(userId, workspaceId, WorkspaceRole.Member, cancellationToken);

        return result.Entity;
    }

    public async Task<List<WorkspaceAppUser>> InviteUsersToWorkspace(IEnumerable<string> userIds, int workspaceId, CancellationToken cancellationToken = default)
    {
        var idList = userIds.ToList();

        var defaultPermissions = WorkspaceRolePermissions
            .GetDefaultPermissions(WorkspaceRole.Member)
            .ToList();

        var invites = idList.Select(userId => new WorkspaceAppUser
        {
            WorkspaceId = workspaceId,
            UserId = userId,
            Role = WorkspaceRole.Member,
            Permissions = defaultPermissions,
        }).ToList();

        await Context.WorkspaceAppUsers.AddRangeAsync(invites, cancellationToken);

        return invites;
    }

    private async Task SeedDefaultPermissionsAsync(string userId, int workspaceId, WorkspaceRole role, CancellationToken cancellationToken = default)
    {
        var user = await Context.WorkspaceAppUsers
            .Where(x => x.UserId == userId && x.WorkspaceId == workspaceId)
            .FirstOrDefaultAsync(cancellationToken);

        var defaultPermissions = WorkspaceRolePermissions
            .GetDefaultPermissions(role)
            .ToList();

        user?.Permissions = defaultPermissions;
    }

    public async Task<List<WorkspaceAppUser>> RemoveUsersFromWorkspace(IEnumerable<string> userIds, int workspaceId, CancellationToken cancellationToken = default)
    {
        var usersToRemove = await Context.WorkspaceAppUsers
            .Include(item => item.User)
            .Where(item => item.WorkspaceId == workspaceId && userIds.Contains(item.UserId))
            .ToListAsync(cancellationToken);

        Context.WorkspaceAppUsers.RemoveRange(usersToRemove);

        return usersToRemove;
    }

    public Task<AppUser?> GetByEmail(string email, bool isReadonly = false, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(email)) throw new ArgumentNullException(nameof(email));

        var match = email.Trim().IdentityNormalize();

        return Entities.IsReadonly(isReadonly).FirstOrDefaultAsync(x => x.NormalizedEmail == match, cancellationToken);
    }

    public Task<List<AppUser>> GetByEmailRange(IEnumerable<string> emails, bool isReadonly = false, CancellationToken cancellationToken = default)
    {
        var values = emails.Select(email => email.Trim().IdentityNormalize());

        return Entities
            .Where(x => values.Contains(x.NormalizedEmail))
            .IsReadonly(isReadonly)
            .ToListAsync(cancellationToken);
    }

    public Task<string?> GetUserIdByEmail(string email, bool isReadonly = false, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(email)) throw new ArgumentNullException(nameof(email));

        var match = email.Trim().IdentityNormalize();

        return Entities
            .Where(user => user.NormalizedEmail == match)
            .Select(user => user.Id)
            .IsReadonly(isReadonly)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<bool> IsUserInWorkspace(string userId, int workspaceId, CancellationToken cancellationToken = default)
    {
        return Context.WorkspaceAppUsers
            .AnyAsync(x => x.UserId == userId && x.WorkspaceId == workspaceId, cancellationToken);
    }

    public Task<bool> IsUserInWorkspace(string userId, string workspaceKey, CancellationToken cancellationToken = default)
    {
        return Context.WorkspaceAppUsers
            .AnyAsync(x => x.UserId == userId && x.Workspace.Slug == workspaceKey, cancellationToken);
    }

    public Task<List<AppUser>> IsUserInWorkspaceRange(IEnumerable<string> userIds, int workspaceId, CancellationToken cancellationToken = default)
    {
        return Context.WorkspaceAppUsers
            .Where(x => x.WorkspaceId == workspaceId && userIds.Contains(x.UserId))
            .Include(x => x.User)
            .Select(x => x.User)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public Task<List<UserAvatar>> GetUserAvatars(IEnumerable<string> userIds, int workspaceId, CancellationToken cancellationToken = default)
    {
        return Context.WorkspaceAppUsers
            .Where(x => x.WorkspaceId == workspaceId && userIds.Contains(x.UserId))
            .Select(x => new UserAvatar
            {
                Id = x.UserId,
                DisplayName = x.User.DisplayName,
                ProfilePictureUrl = x.User.PictureUrl,
            })
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}
