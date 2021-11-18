using System.Collections.Generic;
using System.Threading.Tasks;

using Netptune.Core.Entities;
using Netptune.Core.Repositories.Common;

namespace Netptune.Core.Repositories;

public interface IWorkspaceRepository : IRepository<Workspace, int>
{
    Task<int?> GetIdBySlug(string slug);

    Task<Workspace> GetBySlug(string slug, bool isReadonly = false);

    Task<Workspace> GetBySlugWithTasks(string slug, bool includeRelated, bool isReadonly = false);

    Task<List<Workspace>> GetUserWorkspaces(string userId);

    Task<bool> Exists(string slug);
}