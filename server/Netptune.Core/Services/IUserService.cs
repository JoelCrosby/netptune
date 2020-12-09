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

        Task<List<UserViewModel>> GetAll();

        Task<UserViewModel> GetByEmail(string email);

        Task<List<WorkspaceUserViewModel>> GetWorkspaceUsers();

        Task<UserViewModel> Update(AppUser user);

        Task<ClientResponse> InviteUserToWorkspace(string userId);

        Task<ClientResponse> InviteUsersToWorkspace(IEnumerable<string> emailAddresses, bool onlyNewUsers = false);

        Task<ClientResponse> RemoveUsersFromWorkspace(IEnumerable<string> emailAddresses);
    }
}
