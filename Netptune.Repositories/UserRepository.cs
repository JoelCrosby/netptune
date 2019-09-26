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

        public async Task<IList<AppUser>> GetWorkspaceUsers(int workspaceId)
        {
            return await (from workspaceAppUsers in Context.WorkspaceAppUsers
                          where workspaceAppUsers.WorkspaceId == workspaceId
                          select workspaceAppUsers.User).ToListAsync();
        }

        public async Task<AppUser> Update(AppUser user, string currentUserId)
        {
            var updatedUser = await Context.AppUsers.FirstOrDefaultAsync(x => x.Id == user.Id);

            if (updatedUser == null)
            {
                return null;
            }

            updatedUser.PhoneNumber = user.PhoneNumber;
            updatedUser.Firstname = user.Firstname;
            updatedUser.Lastname = user.Lastname;

            return updatedUser;
        }

        public async Task<WorkspaceAppUser> InviteUserToWorkspace(string userId, int workspaceId)
        {
            if (string.IsNullOrEmpty(userId)) throw new ArgumentNullException(nameof(userId));

            var invite = new WorkspaceAppUser()
            {
                WorkspaceId = workspaceId,
                UserId = userId
            };

            var result = await Context.WorkspaceAppUsers.AddAsync(invite);

            return result.Entity;
        }

        public async Task<AppUser> GetByEmail(string email)
        {
            return await Context.AppUsers.FirstOrDefaultAsync(x => x.Email == email);
        }

        public async Task<bool> IsUserInWorkspace(string userId, int workspaceId)
        {
            return await Context.WorkspaceAppUsers
                .AnyAsync(x => x.UserId == userId && x.WorkspaceId == workspaceId);
        }
    }
}