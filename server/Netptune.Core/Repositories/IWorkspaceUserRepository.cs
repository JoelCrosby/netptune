using Netptune.Core.Models;
using Netptune.Core.Relationships;
using Netptune.Core.Repositories.Common;

namespace Netptune.Core.Repositories;

public interface IWorkspaceUserRepository : IRepository<WorkspaceAppUser, int>
{
    Task<UserPermissions?> GetUserPermissions(string userId, string workspaceKey, bool isReadOnly = true);

    Task SetUserPermissions(string userId, int workspaceId, IEnumerable<string> permissions);
}
