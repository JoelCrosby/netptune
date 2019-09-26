using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;

using Netptune.Core.Repositories;
using Netptune.Core.Repositories.Common;
using Netptune.Entities.Contexts;
using Netptune.Models;
using Netptune.Models.Relationships;
using Netptune.Repositories.Common;

namespace Netptune.Repositories
{
    public class WorkspaceRepository : Repository<DataContext, Workspace, int>, IWorkspaceRepository
    {
        public WorkspaceRepository(DataContext context, IDbConnectionFactory connectionFactory)
            : base(context, connectionFactory)
        {
        }

        public async Task<IEnumerable<Workspace>> GetWorkspaces(AppUser user)
        {
            // Load the relationship table.
            Entities.Include(m => m.WorkspaceUsers).ThenInclude(e => e.User);

            // Select workspaces
            return await Context.WorkspaceAppUsers
                .Where(x => x.User.Id == user.Id)
                .Select(w => w.Workspace)
                .Where(x => !x.IsDeleted)
                .ToListAsync();
        }

        public async Task<Workspace> UpdateWorkspace(Workspace workspace, AppUser user)
        {
            if (workspace == null) throw new ArgumentNullException(nameof(workspace));

            if (user == null) throw new ArgumentNullException(nameof(user));

            var result = await Entities
                .FirstOrDefaultAsync(x => x.Id == workspace.Id);

            if (result == null) return null;

            result.Name = workspace.Name;
            result.Description = workspace.Description;

            if (workspace.IsDeleted)
            {
                result.IsDeleted = true;
                result.DeletedByUserId = user.Id;
            }

            result.UpdatedAt = DateTime.UtcNow;

            return result;
        }

        public async Task<Workspace> AddWorkspace(Workspace workspace, AppUser user)
        {
            workspace.CreatedByUserId = user.Id;
            workspace.OwnerId = user.Id;

            var result = await Entities.AddAsync(workspace);

            // Need to explicitly load the navigation property context.
            // other wise the workspace.WorkspaceUsers list will return null.
            Entities.Include(m => m.WorkspaceUsers);

            var relationship = new WorkspaceAppUser
            {
                UserId = user.Id,
                WorkspaceId = workspace.Id
            };

            await Context.WorkspaceAppUsers.AddAsync(relationship);

            return result.Entity;
        }

        public async Task<Workspace> DeleteWorkspace(int id)
        {
            var workspace = await Entities.FindAsync(id);

            if (workspace == null) return null;

            Entities.Remove(workspace);

            return workspace;
        }
    }
}