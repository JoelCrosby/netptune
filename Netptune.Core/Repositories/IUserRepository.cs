using Netptune.Core.Entities;
using Netptune.Core.Relationships;
using Netptune.Core.Repositories.Common;

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Netptune.Core.Repositories
{
    public interface IUserRepository : IRepository<AppUser, string>
    {
        Task<AppUser> GetByEmail(string email);

        Task<List<AppUser>> GetWorkspaceUsers(string workspaceSlug);

        Task<WorkspaceAppUser> InviteUserToWorkspace(string userId, int workspaceId);

        Task<bool> IsUserInWorkspace(string userId, int workspaceId);
    }
}