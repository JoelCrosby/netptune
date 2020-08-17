using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;

using Netptune.Core.Entities;
using Netptune.Core.Repositories;
using Netptune.Core.Repositories.Common;
using Netptune.Entities.Contexts;
using Netptune.Repositories.Common;

namespace Netptune.Repositories
{
    public class WorkspaceRepository : AuditableRepository<DataContext, Workspace, int>, IWorkspaceRepository
    {
        public WorkspaceRepository(DataContext context, IDbConnectionFactory connectionFactory)
            : base(context, connectionFactory)
        {
        }

        public Task<Workspace> GetBySlug(string slug, bool isReadonly = false)
        {
            return Entities
                .IsReadonly(isReadonly)
                .FirstOrDefaultAsync(workspace => workspace.Slug == slug && !workspace.IsDeleted);
        }

        public Task<Workspace> GetBySlugWithTasks(string slug, bool includeRelated, bool isReadonly = false)
        {
            if (includeRelated)
            {
                return Entities
                    .IsReadonly(isReadonly)
                    .Include(workspace => workspace.Projects)
                    .ThenInclude(project => project.ProjectTasks)
                    .FirstOrDefaultAsync(workspace => workspace.Slug == slug && !workspace.IsDeleted);
            }

            return Entities.FirstOrDefaultAsync(workspace => workspace.Slug == slug && !workspace.IsDeleted);
        }

        public Task<List<Workspace>> GetWorkspaces(AppUser user)
        {
            return Context.WorkspaceAppUsers
                .Where(x => x.UserId == user.Id)
                .Select(w => w.Workspace)
                .Where(x => !x.IsDeleted)
                .ToListAsync();
        }

        public Task<bool> Exists(string slug)
        {
            return Entities.AnyAsync(workspace => workspace.Slug == slug && !workspace.IsDeleted);
        }
    }
}
