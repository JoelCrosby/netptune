using System.Threading.Tasks;
using Netptune.Core.Cache.Common;

namespace Netptune.Core.Cache
{
    public class WorkspaceUserKey
    {
        public string WorkspaceKey { get; set; }

        public string UserId { get; set; }
    }

    public interface IWorkspaceUserCache : IEntityCache<bool, WorkspaceUserKey>
    {
        Task<bool> IsUserInWorkspace(string userId, string workspaceKey);
    }
}
