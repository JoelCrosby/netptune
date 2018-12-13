using Netptune.Models.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Netptune.Models.Repositories
{
    public interface IUserRepository
    {
        Task<RepoResult<AppUser>> GetUserAsync(string userId);

        Task<RepoResult<AppUser>> GetUserByEmailAsync(string email);

        Task<RepoResult<IEnumerable<AppUser>>> GetWorkspaceUsersAsync(int workspaceId);

        Task<RepoResult<AppUser>> UpdateUserAsync(AppUser user, string currentUserId);

        Task<RepoResult<AppUser>> InviteUserToWorkspaceAsync(string userId, int workspaceId);

    }
}