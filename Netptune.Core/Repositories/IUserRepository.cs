using System.Collections.Generic;
using System.Threading.Tasks;

using Netptune.Core.Repositories.Common;
using Netptune.Models;
using Netptune.Models.Relationships;

namespace Netptune.Core.Repositories
{
    public interface IUserRepository : IRepository<AppUser, string>
    {
        Task<AppUser> GetByEmail(string email);

        Task<List<AppUser>> GetWorkspaceUsers(int workspaceId);

        Task<WorkspaceAppUser> InviteUserToWorkspace(string userId, int workspaceId);

        Task<bool> IsUserInWorkspace(string userId, int workspaceId);
    }
}