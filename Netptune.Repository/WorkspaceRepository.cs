using Microsoft.EntityFrameworkCore;
using Netptune.Entities.Entites;
using Netptune.Entities.Contexts;
using Netptune.Entities.Entites.Relationships;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Netptune.Repository.Interfaces;
using Netptune.Repositories.Models;

namespace Netptune.Repository
{
    public class WorkspaceRepository : IWorkspaceRepository
    {
        private readonly DataContext _context;

        public WorkspaceRepository(DataContext dataContext)
        {
            _context = dataContext;
        }

        public async Task<RepoResult<IEnumerable<Workspace>>> GetWorkspaces(AppUser user)
        {

            // Load the relationship table.
            _context.Workspaces.Include(m => m.WorkspaceUsers).ThenInclude(e => e.User);

            // Select workspaces
            var result = await _context.WorkspaceAppUsers
                .Where(x => x.User.Id == user.Id)
                .Select(w => w.Workspace)
                .Where(x => !x.IsDeleted)
                .ToListAsync();

            return RepoResult<IEnumerable<Workspace>>.Ok(result);
        }

        public async Task<RepoResult<Workspace>> GetWorkspace(int id)
        {

            var result = await _context.Workspaces.FindAsync(id);

            if (result == null) return RepoResult<Workspace>.NotFound();

            return RepoResult<Workspace>.Ok(result);
        }

        public async Task<RepoResult<Workspace>> UpdateWorkspace(Workspace workspace, AppUser user)
        {

            if (workspace == null)
            {
                return RepoResult<Workspace>.BadRequest();
            }

            var result = await _context.Workspaces
                .FirstOrDefaultAsync(x => x.Id == workspace.Id);

            if (result == null) return RepoResult<Workspace>.NotFound();

            result.Name = workspace.Name;
            result.Description = workspace.Description;
            
            if (workspace.IsDeleted)
            {
                result.IsDeleted = true;
                result.DeletedByUserId = user.Id;
            }

            result.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return RepoResult<Workspace>.Ok(result);
        }

        public async Task<RepoResult<Workspace>> AddWorkspace(Workspace workspace, AppUser user)
        {

            workspace.CreatedByUserId = user.Id;
            workspace.OwnerId = user.Id;

            var result = await _context.Workspaces.AddAsync(workspace);

            // Need to explicitly load the navigation property context.
            // other wise the workspace.WorkspaceUsers list will return null.
            _context.Workspaces.Include(m => m.WorkspaceUsers);

            var relationship = new WorkspaceAppUser
            {
                UserId = user.Id,
                WorkspaceId = workspace.Id
            };

            await _context.WorkspaceAppUsers.AddAsync(relationship);

            await _context.SaveChangesAsync();

            return RepoResult<Workspace>.Ok(result.Entity);
        }

        public async Task<RepoResult<Workspace>> DeleteWorkspace(int id)
        {
            var workspace = await _context.Workspaces.FindAsync(id);
            if (workspace == null) return RepoResult<Workspace>.NotFound();

            _context.Workspaces.Remove(workspace);

            await _context.SaveChangesAsync();

            return RepoResult<Workspace>.NoContent();

        }
    }
}