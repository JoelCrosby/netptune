using System.Collections.Generic;
using System.Threading.Tasks;

using Netptune.Models;
using Netptune.Models.Relationships;
using Netptune.Models.ViewModels.Users;

namespace Netptune.Core.Services
{
    public interface IUserService
    {
        Task<UserViewModel> Get(string userId);

        Task<UserViewModel> GetByEmail(string email);

        Task<List<UserViewModel>> GetWorkspaceUsers(string workspaceSlug);

        Task<UserViewModel> Update(AppUser user, string userId);

        Task<WorkspaceAppUser> InviteUserToWorkspace(string userId, int workspaceId);
    }
}
