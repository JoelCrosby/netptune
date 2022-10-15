using System.Collections.Generic;
using System.Threading.Tasks;

using Netptune.Core.Entities;
using Netptune.Core.Models;
using Netptune.Core.Relationships;
using Netptune.Core.Repositories.Common;

namespace Netptune.Core.Repositories;

public interface IUserRepository : IRepository<AppUser, string>
{
    Task<AppUser?> GetByEmail(string email, bool isReadonly = false);

    Task<string?> GetUserIdByEmail(string email, bool isReadonly = false);

    Task<List<AppUser>> GetByEmailRange(IEnumerable<string> emails, bool isReadonly = false);

    Task<List<AppUser>> GetWorkspaceUsers(string workspaceKey, bool isReadonly = false);

    Task<WorkspaceAppUser> InviteUserToWorkspace(string userId, int workspaceId);

    Task<List<WorkspaceAppUser>> InviteUsersToWorkspace(IEnumerable<string> userIds, int workspaceId);

    Task<List<WorkspaceAppUser>> RemoveUsersFromWorkspace(IEnumerable<string> userIds, int workspaceId);

    Task<bool> IsUserInWorkspace(string userId, int workspaceId);

    Task<bool> IsUserInWorkspace(string userId, string workspaceKey);

    Task<List<string>> IsUserInWorkspaceRange(IEnumerable<string> userIds, int workspaceId);

    Task<List<UserAvatar>> GetUserAvatars(IEnumerable<string> userIds, int workspaceId);
}
