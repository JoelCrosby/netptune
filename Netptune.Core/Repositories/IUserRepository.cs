using System.Collections.Generic;
using System.Threading.Tasks;

using Netptune.Core.Entities;
using Netptune.Core.Relationships;
using Netptune.Core.Repositories.Common;

namespace Netptune.Core.Repositories
{
    public interface IUserRepository : IRepository<AppUser, string>
    {
        Task<AppUser> GetByEmail(string email);

        Task<string> GetUserIdByEmail(string email);

        Task<List<AppUser>> GetWorkspaceUsers(string workspaceSlug);

        Task<WorkspaceAppUser> InviteUserToWorkspace(string userId, int workspaceId);

        Task<bool> IsUserInWorkspace(string userId, int workspaceId);
    }
}
