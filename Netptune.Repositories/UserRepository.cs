using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;

using Netptune.Core.Repositories;
using Netptune.Core.Repositories.Common;
using Netptune.Entities.Contexts;
using Netptune.Models;
using Netptune.Models.Relationships;
using Netptune.Repositories.Common;

namespace Netptune.Repositories
{
    public class UserRepository : Repository<DataContext, AppUser, string>, IUserRepository
    {
        public UserRepository(DataContext context, IDbConnectionFactory connectionFactory)
            : base(context, connectionFactory)
        {
        }

        public async Task<List<AppUser>> GetWorkspaceUsers(string workspaceSlug)
        {
            var result = await Context.Workspaces
                .Include(workspace => workspace.WorkspaceUsers)
                .ThenInclude(workspaceUser => workspaceUser.User)
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

        public Task<AppUser> GetByEmail(string email)
        {
            return Entities.FirstOrDefaultAsync(x => x.Email == email);
        }

        public Task<bool> IsUserInWorkspace(string userId, int workspaceId)
        {
            return Context.WorkspaceAppUsers
                .AnyAsync(x => x.UserId == userId && x.WorkspaceId == workspaceId);
        }
    }
}