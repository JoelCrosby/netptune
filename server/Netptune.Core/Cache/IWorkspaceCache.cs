using System.Threading.Tasks;

using Netptune.Core.Cache.Common;
using Netptune.Core.Entities;

namespace Netptune.Core.Cache;

public interface IWorkspaceCache : IEntityCache<Workspace, string>
{
    Task<int?> GetIdBySlug(string slug);
}