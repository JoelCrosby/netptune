using Netptune.Models.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Netptune.Models.Repositories
{
    public interface IUserRepository
    {
        AppUser GetUser(string userId);

        IEnumerable<AppUser> GetWorkspaceUsers(int workspaceId);

        Task<AppUser> UpdateUserAsync(AppUser user);

        Task<AppUser> InviteUserToWorkspace(string userId, int workspaceId);

    }
}