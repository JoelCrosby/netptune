using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;

using Netptune.Core.Entities;
using Netptune.Core.Relationships;
using Netptune.Core.Repositories;
using Netptune.Core.Repositories.Common;
using Netptune.Entities.Contexts;
using Netptune.Repositories.Common;

namespace Netptune.Repositories
{
    public class UserRepository : Repository<DataContext, AppUser, string>, IUserRepository
    {
        public UserRepository(DataContext context, IDbConnectionFactory connectionFactory)
            : base(context, connectionFactory)
        {
        }

        public async Task<List<AppUser>> GetWorkspaceUsers(string workspaceSlug, bool isReadonly = false)
        {
            var result = await Context.Workspaces
                .Include(workspace => workspace.WorkspaceUsers)
                .ThenInclude(workspaceUser => workspaceUser.User)
                .IsReadonly(isReadonly)
                .FirstOrDefaultAsync(workspace => workspace.Slug == workspaceSlug);

            return result.WorkspaceUsers.Select(x => x.User).ToList();
        }

        public async Task<WorkspaceAppUser> InviteUserToWorkspace(string userId, int workspaceId)
        {
            if (string.IsNullOrEmpty(userId)) throw new ArgumentNullException(nameof(userId));

            var invite = new WorkspaceAppUser
            {
                WorkspaceId = workspaceId,
                UserId = userId
            };

            var result = await Context.WorkspaceAppUsers.AddAsync(invite);

            return result.Entity;
        }

        public async Task<List<WorkspaceAppUser>> InviteUsersToWorkspace(IEnumerable<string> userIds, int workspaceId)
        {
            var invites = userIds.Select(userId => new WorkspaceAppUser
            {
                WorkspaceId = workspaceId,
                UserId = userId
            }).ToList();

            await Context.WorkspaceAppUsers.AddRangeAsync(invites);

            return invites;
        }

        public async Task<List<WorkspaceAppUser>> RemoveUsersFromWorkspace(IEnumerable<string> userIds, int workspaceId)
        {
            var toRemove = await Context.WorkspaceAppUsers
                .Where(item => item.WorkspaceId == workspaceId && userIds.Contains(item.UserId))
                .ToListAsync();

            Context.WorkspaceAppUsers.RemoveRange(toRemove);

            return toRemove;
        }

        public Task<AppUser> GetByEmail(string email, bool isReadonly = false)
        {
            if (string.IsNullOrEmpty(email)) throw new ArgumentNullException(nameof(email));

            var match = email.Trim().Normalize();

            return Entities.IsReadonly(isReadonly).FirstOrDefaultAsync(x => x.NormalizedEmail == match);
        }

        public Task<List<AppUser>> GetByEmailRange(IEnumerable<string> emails, bool isReadonly = false)
        {
            var values = emails.Select(email => email.Trim().ToUpper().Normalize());

            return Entities
                .Where(x => values.Contains(x.NormalizedEmail))
                .IsReadonly(isReadonly)
                .ToListAsync();
        }

        public Task<string> GetUserIdByEmail(string email, bool isReadonly = false)
        {
            if (string.IsNullOrEmpty(email)) throw new ArgumentNullException(nameof(email));

            var match = email.Trim().Normalize();

            return Entities
                .Where(user => user.NormalizedEmail == match)
                .Select(user => user.Id)
                .IsReadonly(isReadonly)
                .FirstOrDefaultAsync();
        }

        public Task<bool> IsUserInWorkspace(string userId, int workspaceId)
        {
            return Context.WorkspaceAppUsers
                .AnyAsync(x => x.UserId == userId && x.WorkspaceId == workspaceId);
        }

        public Task<List<string>> IsUserInWorkspaceRange(IEnumerable<string> userIds, int workspaceId)
        {
            return Context.WorkspaceAppUsers
                .Where(x => x.WorkspaceId == workspaceId && userIds.Contains(x.UserId))
                .Select(x => x.UserId)
                .AsNoTracking()
                .ToListAsync();
        }
    }
}
