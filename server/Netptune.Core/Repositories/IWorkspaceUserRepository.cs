using Netptune.Core.Models;
using Netptune.Core.Relationships;
using Netptune.Core.Repositories.Common;

namespace Netptune.Core.Repositories;

public interface IWorkspaceUserRepository : IRepository<WorkspaceAppUser, int>
{
    Task<UserPermissions?> GetUserPermissions(string userId, string workspaceKey, bool isReadOnly = true, CancellationToken cancellationToken = default);

    Task SetUserPermissions(string userId, int workspaceId, IEnumerable<string> permissions, CancellationToken cancellationToken = default);

    Task<List<string>> GetWorkspaceUserIds(int workspaceId, CancellationToken cancellationToken = default);

    Task<Dictionary<int, List<string>>> GetWorkspaceUserIdsByWorkspaceIds(IEnumerable<int> workspaceIds, CancellationToken cancellationToken = default);
}
