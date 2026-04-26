using System.Linq.Expressions;

using Microsoft.EntityFrameworkCore;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Repositories;
using Netptune.Core.Repositories.Common;
using Netptune.Core.ViewModels.Projects;
using Netptune.Entities.Contexts;
using Netptune.Repositories.Common;

namespace Netptune.Repositories;

public class ProjectRepository : WorkspaceEntityRepository<DataContext, Project, int>, IProjectRepository
{
    public ProjectRepository(DataContext dataContext, IDbConnectionFactory connectionFactories)
        : base(dataContext, connectionFactories)
    {
    }

    public Task<Project?> GetWithIncludes(int id)
    {
        return Entities
            .Include(item => item.Owner)
            .Include(item => item.ProjectBoards)
            .AsSplitQuery()
            .FirstOrDefaultAsync(item => item.Id == id);
    }

    public Task<List<ProjectViewModel>> GetProjects(string workspaceKey)
    {
        return Entities
            .Where(project => project.Workspace.Slug == workspaceKey && !project.IsDeleted)
            .AsNoTracking()
            .Select(ProjectToViewModel())
            .ToListAsync();
    }

    public Task<ProjectViewModel?> GetProjectViewModel(int id)
    {
        return Entities
            .Where(project => project.Id == id && !project.IsDeleted)
            .AsNoTracking()
            .Select(ProjectToViewModel())
            .FirstOrDefaultAsync();
    }

    public Task<ProjectViewModel?> GetProjectViewModel(string key, int workspaceId)
    {
        return Entities
            .Where(project => !project.IsDeleted && project.Key == key && project.WorkspaceId == workspaceId)
            .AsNoTracking()
            .Select(ProjectToViewModel())
            .FirstOrDefaultAsync();
    }

    public async Task<bool> IsProjectKeyAvailable(string key, int workspaceId)
    {
        var exists = await Entities
            .AsNoTracking()
            .AnyAsync(project => project.WorkspaceId == workspaceId && project.Key == key);

        return !exists;
    }

    private static Expression<Func<Project, ProjectViewModel>> ProjectToViewModel()
    {
        return x => new ProjectViewModel
        {
            Id = x.Id,
            Key = x.Key,
            Name = x.Name,
            Description = x.Description,
            RepositoryUrl = x.RepositoryUrl,
            WorkspaceId = x.WorkspaceId,
            OwnerDisplayName = string.IsNullOrEmpty(x.Owner!.Firstname) && string.IsNullOrEmpty(x.Owner.Lastname)
                ? x.Owner.UserName!
                : x.Owner.Firstname + " " + x.Owner.Lastname,
            UpdatedAt = x.UpdatedAt,
            CreatedAt = x.CreatedAt,
            Color = x.MetaInfo != null ? x.MetaInfo.Color : null,
            DefaultBoardIdentifier = x.ProjectBoards
                .Where(b => b.BoardType == BoardType.Default)
                .Select(b => b.Identifier)
                .FirstOrDefault(),
        };
    }

    public Task<string> GenerateProjectKey(string projectName, int workspaceId)
    {
        const int keyLength = 4;
        var key = projectName[..keyLength].ToLowerInvariant();

        async Task<string> TryGetKey(string currentKey, int currentKeyLength)
        {
            while (true)
            {
                var isAvailable = await IsProjectKeyAvailable(currentKey, workspaceId);
                if (isAvailable) return currentKey;
                var nextKey = projectName[..(currentKeyLength + 1)].ToLowerInvariant();
                currentKey = nextKey;
                currentKeyLength++;
            }
        }

        return TryGetKey(key, keyLength);
    }
}
