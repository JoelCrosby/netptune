using Netptune.Entities.Entites;
using Netptune.Repositories.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Netptune.Repository.Interfaces
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