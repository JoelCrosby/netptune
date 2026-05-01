using Netptune.Core.Authorization;
using Netptune.Core.Entities;
using Netptune.Core.Models;
using Netptune.Core.Relationships;
using Netptune.Core.Repositories.Common;

namespace Netptune.Core.Repositories;

public interface IUserRepository : IRepository<AppUser, string>
{
    Task<AppUser?> GetByEmail(string email, bool isReadonly = false, CancellationToken cancellationToken = default);

    Task<string?> GetUserIdByEmail(string email, bool isReadonly = false, CancellationToken cancellationToken = default);

    Task<List<AppUser>> GetByEmailRange(IEnumerable<string> emails, bool isReadonly = false, CancellationToken cancellationToken = default);

    Task<List<AppUser>> GetWorkspaceUsers(string workspaceKey, bool isReadonly = false, CancellationToken cancellationToken = default);

    Task<List<WorkspaceAppUser>> GetWorkspaceAppUsers(string workspaceKey, bool isReadonly = false, CancellationToken cancellationToken = default);

    Task<WorkspaceRole?> GetUserWorkspaceRole(string userId, string workspaceKey, CancellationToken cancellationToken = default);

    Task<WorkspaceAppUser> InviteUserToWorkspace(string userId, int workspaceId, CancellationToken cancellationToken = default);

    Task<List<WorkspaceAppUser>> InviteUsersToWorkspace(IEnumerable<string> userIds, int workspaceId, CancellationToken cancellationToken = default);

    Task<List<WorkspaceAppUser>> RemoveUsersFromWorkspace(IEnumerable<string> userIds, int workspaceId, CancellationToken cancellationToken = default);

    Task<bool> IsUserInWorkspace(string userId, int workspaceId, CancellationToken cancellationToken = default);

    Task<bool> IsUserInWorkspace(string userId, string workspaceKey, CancellationToken cancellationToken = default);

    Task<List<AppUser>> IsUserInWorkspaceRange(IEnumerable<string> userIds, int workspaceId, CancellationToken cancellationToken = default);

    Task<List<UserAvatar>> GetUserAvatars(IEnumerable<string> userIds, int workspaceId, CancellationToken cancellationToken = default);
}
