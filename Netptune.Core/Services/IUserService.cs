using Netptune.Core.Relationships;
using Netptune.Core.ViewModels.Users;

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Netptune.Core.Services
{
    public interface IUserService
    {
        Task<UserViewModel> Get(string userId);

        Task<UserViewModel> GetByEmail(string email);

        Task<List<UserViewModel>> GetWorkspaceUsers(string workspaceSlug);

        Task<UserViewModel> Update(AppUser user);

        Task<WorkspaceAppUser> InviteUserToWorkspace(string userId, int workspaceId);
    }
}
