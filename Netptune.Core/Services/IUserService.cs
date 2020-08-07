using System.Collections.Generic;
using System.Threading.Tasks;

using Netptune.Core.Entities;
using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.Users;

namespace Netptune.Core.Services
{
    public interface IUserService
    {
        Task<UserViewModel> Get(string userId);

        Task<UserViewModel> GetByEmail(string email);

        Task<List<UserViewModel>> GetWorkspaceUsers(string workspaceSlug);

        Task<UserViewModel> Update(AppUser user);

        Task<ClientResponse> InviteUserToWorkspace(string userId, int workspaceId);
    }
}
