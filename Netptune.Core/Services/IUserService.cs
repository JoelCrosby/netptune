using System.Collections.Generic;
using System.Threading.Tasks;

using Netptune.Core.Models;
using Netptune.Models;
using Netptune.Models.Relationships;

namespace Netptune.Core.Services
{
    public interface IUserService
    {
        Task<ServiceResult<AppUser>> Get(string userId);

        Task<ServiceResult<AppUser>> GetByEmail(string email);

        Task<ServiceResult<IList<AppUser>>> GetWorkspaceUsers(int workspaceId);

        Task<ServiceResult<AppUser>> Update(AppUser user, string userId);

        Task<ServiceResult<WorkspaceAppUser>> InviteUserToWorkspace(string userId, int workspaceId);
    }
}
