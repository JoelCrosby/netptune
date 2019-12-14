using System.Collections.Generic;
using System.Threading.Tasks;

using Netptune.Models;
using Netptune.Models.Relationships;
using Netptune.Models.ViewModels.Users;

namespace Netptune.Core.Services
{
    public interface IUserService
    {
        Task<AppUser> Get(string userId);

        Task<AppUser> GetByEmail(string email);

        Task<List<UserViewModel>> GetWorkspaceUsers(string workspaceSlug);

        Task<AppUser> Update(AppUser user, string userId);

        Task<WorkspaceAppUser> InviteUserToWorkspace(string userId, int workspaceId);
    }
}
