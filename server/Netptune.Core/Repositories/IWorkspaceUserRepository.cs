using Netptune.Core.Relationships;
using Netptune.Core.Repositories.Common;

namespace Netptune.Core.Repositories;

public interface IWorkspaceUserRepository : IRepository<WorkspaceAppUser, int>
{
    Task<HashSet<string>?> GetUserPermissions(string userId, string workspaceKey);

    Task SetUserPermissions(string userId, int workspaceId, IEnumerable<string> permissions);
}
