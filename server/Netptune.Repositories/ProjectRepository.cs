using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Repositories;
using Netptune.Core.Repositories.Common;
using Netptune.Core.ViewModels.Projects;
using Netptune.Entities.Contexts;
using Netptune.Repositories.Common;

namespace Netptune.Repositories
{
    public class ProjectRepository : Repository<DataContext, Project, int>, IProjectRepository
    {
        public ProjectRepository(DataContext dataContext, IDbConnectionFactory connectionFactories)
            : base(dataContext, connectionFactories)
        {
        }

        public async Task<List<ProjectViewModel>> GetProjects(string workspaceKey)
        {
            var workspace = await Context.Workspaces.FirstOrDefaultAsync(item => item.Slug == workspaceKey);

            if (workspace is null) return new List<ProjectViewModel>();

            var workspaceId = workspace.Id;

            Entities.Include(task => task.Owner).ThenInclude(x => x.UserName);

            return await Entities
                .Where(project => project.WorkspaceId == workspaceId && !project.IsDeleted)
                .Include(project => project.Workspace)
                .Include(project => project.Owner)
                .Include(project => project.ProjectBoards)
                .AsNoTracking()
                .Select(project => GetViewModel(project))
                .ToListAsync();
        }

        public Task<ProjectViewModel> GetProjectViewModel(int id)
        {
            Entities.Include(task => task.Owner).ThenInclude(x => x.UserName);

            return Entities
                .Where(project => project.Id == id && !project.IsDeleted)
                .Include(project => project.Workspace)
                .Include(project => project.Owner)
                .Include(project => project.ProjectBoards)
                .AsNoTracking()
                .Select(project => GetViewModel(project))
                .FirstOrDefaultAsync();
        }

        public Task<ProjectViewModel> GetProjectViewModel(string key, int workspaceId)
        {
            Entities.Include(task => task.Owner).ThenInclude(x => x.UserName);

            return Entities
                .Where(project => project.Key == key && !project.IsDeleted)
                .Include(project => project.Workspace)
                .Include(project => project.Owner)
                .Include(project => project.ProjectBoards)
                .AsNoTracking()
                .Select(project => GetViewModel(project))
                .FirstOrDefaultAsync();
        }

        public Task<bool> IsProjectKeyAvailable(string key, int workspaceId)
        {
            return Entities
                    .AsNoTracking()
                    .Where(project => project.WorkspaceId == workspaceId)
                    .AllAsync(project => project.Key != key);
        }

        private static ProjectViewModel GetViewModel(Project project)
        {
            var defaultBoard = project.ProjectBoards?.FirstOrDefault(board => board.BoardType == BoardType.Default);
            var identifier = defaultBoard?.Identifier;

            return new ProjectViewModel
            {
                Id = project.Id,
                Key = project.Key,
                Name = project.Name,
                Description = project.Description,
                RepositoryUrl = project.RepositoryUrl,
                Color = project.MetaInfo.Color,
                WorkspaceId = project.WorkspaceId,
                OwnerDisplayName = project.Owner.DisplayName,
                UpdatedAt = project.UpdatedAt,
                CreatedAt = project.CreatedAt,
                DefaultBoardIdentifier = identifier,
            };
        }

        public Task<string> GenerateProjectKey(string projectName, int workspaceId)
        {
            const int keyLength = 4;
            var key = projectName.Substring(0, keyLength).ToLowerInvariant();

            async Task<string> TryGetKey(string currentKey, int currentKeyLength)
            {
                while (true)
                {
                    var isAvailable = await IsProjectKeyAvailable(currentKey, workspaceId);
                    if (isAvailable) return currentKey;
                    var nextKey = projectName.Substring(0, currentKeyLength + 1).ToLowerInvariant();
                    currentKey = nextKey;
                    currentKeyLength++;
                }
            }

            return TryGetKey(key, keyLength);
        }
    }
}
