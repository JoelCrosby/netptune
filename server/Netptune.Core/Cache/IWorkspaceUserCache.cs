using System.Threading.Tasks;
using Netptune.Core.Cache.Common;

namespace Netptune.Core.Cache;

public record WorkspaceUserKey
{
    public string WorkspaceKey { get; init; } = null!;

    public string UserId { get; init; } = null!;
}

public interface IWorkspaceUserCache : IEntityCache<bool, WorkspaceUserKey>
{
    Task<bool> IsUserInWorkspace(string userId, string workspaceKey);
}
