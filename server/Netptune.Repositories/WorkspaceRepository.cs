using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;

using Netptune.Core.Entities;
using Netptune.Core.Repositories;
using Netptune.Core.Repositories.Common;
using Netptune.Entities.Contexts;
using Netptune.Repositories.Common;

namespace Netptune.Repositories;

public class WorkspaceRepository : AuditableRepository<DataContext, Workspace, int>, IWorkspaceRepository
{
    public WorkspaceRepository(DataContext context, IDbConnectionFactory connectionFactory)
        : base(context, connectionFactory)
    {
    }

    public async Task<int?> GetIdBySlug(string slug)
    {
        var result = await Entities
            .IsReadonly(true)
            .Where(workspace => workspace.Slug == slug && !workspace.IsDeleted)
            .Select(x => x.Id)
            .Take(1)
            .ToListAsync();

        if (result.Any()) return result.FirstOrDefault();

        return null;
    }

    public Task<Workspace?> GetBySlug(string slug, bool isReadonly = false)
    {
        return Entities
            .IsReadonly(isReadonly)
            .FirstOrDefaultAsync(workspace => workspace.Slug == slug && !workspace.IsDeleted);
    }

    public Task<Workspace?> GetBySlugWithTasks(string slug, bool includeRelated, bool isReadonly = false)
    {
        if (includeRelated)
        {
            return Entities
                .IsReadonly(isReadonly)
                .Include(workspace => workspace.Projects)
                .ThenInclude(project => project.ProjectTasks)
                .FirstOrDefaultAsync(workspace => workspace.Slug == slug && !workspace.IsDeleted);
        }

        return Entities
            .IsReadonly(isReadonly)
            .FirstOrDefaultAsync(workspace => workspace.Slug == slug && !workspace.IsDeleted);
    }

    public Task<List<Workspace>> GetUserWorkspaces(string userId)
    {
        return Context.WorkspaceAppUsers
            .Where(x => x.UserId == userId)
            .Select(w => w.Workspace)
            .Where(x => !x.IsDeleted)
            .ToListAsync();
    }

    public Task<bool> Exists(string slug)
    {
        return Entities.AnyAsync(workspace => workspace.Slug == slug);
    }
}
