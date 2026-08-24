using System.Linq.Expressions;

using Dapper;

using Microsoft.EntityFrameworkCore;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Models.ProjectTasks;
using Netptune.Core.Repositories;
using Netptune.Core.Repositories.Common;
using Netptune.Core.Requests;
using Netptune.Core.ViewModels.Projects;
using Netptune.Entities.Contexts;
using Netptune.Repositories.Common;
using Netptune.Repositories.Sql;

namespace Netptune.Repositories;

public class ProjectRepository : WorkspaceEntityRepository<DataContext, Project, int>, IProjectRepository
{
    public ProjectRepository(DataContext dataContext, IDbConnectionFactory connectionFactories)
        : base(dataContext, connectionFactories)
    {
    }

    public Task<Project?> GetWithIncludes(int id, CancellationToken cancellationToken = default)
    {
        return Entities
            .Include(item => item.Owner)
            .Include(item => item.ProjectBoards)
            .Include(item => item.DefaultStatus)
            .AsSplitQuery()
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
    }

    public Task<List<ProjectViewModel>> GetProjects(string workspaceKey, CancellationToken cancellationToken = default, PageRequest? pageRequest = null)
    {
        pageRequest ??= new PageRequest();
        var pagination = pageRequest.GetPagination();
        var query = Entities.Where(project => project.Workspace!.Slug == workspaceKey && !project.IsDeleted);
        var ordered = ApplyProjectOrder(query, pageRequest.SortBy, pageRequest.SortDirection);

        return ordered
            .Skip(pagination.Skip)
            .Take(pagination.PageSize)
            .AsNoTracking()
            .Select(ProjectToViewModel())
            .ToListAsync(cancellationToken);
    }

    private static IQueryable<Project> ApplyProjectOrder(IQueryable<Project> query, string? sortBy, string? sortDirection)
    {
        var descending = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);
        var ownerSortKey = OwnerSortKey();
        var recentActivitySortKey = RecentActivitySortKey();

        IOrderedQueryable<Project> ordered = sortBy?.Trim() switch
        {
            "name" => descending
                ? query.OrderByDescending(project => project.Name)
                : query.OrderBy(project => project.Name),
            "key" => descending
                ? query.OrderByDescending(project => project.Key)
                : query.OrderBy(project => project.Key),
            "description" => descending
                ? query.OrderByDescending(project => project.Description)
                : query.OrderBy(project => project.Description),
            "owner" => descending
                ? query.OrderByDescending(ownerSortKey)
                : query.OrderBy(ownerSortKey),
            "updatedAt" => descending
                ? query.OrderByDescending(recentActivitySortKey)
                : query.OrderBy(recentActivitySortKey),
            _ => query.OrderByDescending(recentActivitySortKey),
        };

        return ordered.ThenByDescending(project => project.Id);
    }

    private static Expression<Func<Project, string?>> OwnerSortKey()
    {
        return project => string.IsNullOrEmpty(project.Owner!.Firstname)
            ? project.Owner.UserName
            : project.Owner.Firstname;
    }

    private static Expression<Func<Project, DateTime>> RecentActivitySortKey()
    {
        return project => project.UpdatedAt ?? project.CreatedAt;
    }

    public Task<List<ProjectViewModel>> GetProjectViewModels(IEnumerable<int> projectIds, CancellationToken cancellationToken = default)
    {
        var idList = projectIds.ToList();

        if (idList.Count == 0) return Task.FromResult(new List<ProjectViewModel>());

        return Entities
            .Where(project => idList.Contains(project.Id) && !project.IsDeleted)
            .AsNoTracking()
            .Select(ProjectToViewModel())
            .ToListAsync(cancellationToken);
    }

    public Task<List<ProjectViewModel>> GetAllProjectViewModels(string workspaceKey, CancellationToken cancellationToken = default)
    {
        return Entities
            .Where(project => project.Workspace!.Slug == workspaceKey && !project.IsDeleted)
            .AsNoTracking()
            .Select(ProjectToViewModel())
            .ToListAsync(cancellationToken);
    }

    public Task<ProjectViewModel?> GetProjectViewModel(int id, CancellationToken cancellationToken = default)
    {
        return Entities
            .Where(project => project.Id == id && !project.IsDeleted)
            .AsNoTracking()
            .Select(ProjectToViewModel())
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<ProjectViewModel?> GetProjectViewModel(string key, int workspaceId, CancellationToken cancellationToken = default)
    {
        return Entities
            .Where(project => !project.IsDeleted && project.Key == key && project.WorkspaceId == workspaceId)
            .AsNoTracking()
            .Select(ProjectToViewModel())
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<TaskCreationProject?> GetTaskCreationProject(int projectId, int workspaceId, CancellationToken cancellationToken = default)
    {
        return Entities
            .Where(project => project.Id == projectId && project.WorkspaceId == workspaceId && !project.IsDeleted)
            .AsNoTracking()
            .Select(project => new TaskCreationProject(project.Id, project.Name, project.WorkspaceId, project.DefaultStatusId))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<int?> ReserveTaskScopeIds(
        int projectId,
        int count,
        CancellationToken cancellationToken = default)
    {

        if (count <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), count, "Scope ID reservation count must be positive.");
        }

        using var connection = ConnectionFactory.StartConnection();
        var command = new CommandDefinition(
            SqlScripts.ReserveTaskScopeIds,
            new { projectId, count },
            cancellationToken: cancellationToken);
        var firstScopeId = await connection.QuerySingleOrDefaultAsync<int?>(command);

        return firstScopeId;
    }

    public async Task<bool> IsProjectKeyAvailable(string key, int workspaceId, CancellationToken cancellationToken = default)
    {
        var exists = await Entities
            .AsNoTracking()
            .AnyAsync(project => project.WorkspaceId == workspaceId && project.Key == key, cancellationToken);

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
            OwnerPictureUrl = x.Owner.PictureUrl,
            UpdatedAt = x.UpdatedAt,
            CreatedAt = x.CreatedAt,
            Color = x.MetaInfo != null ? x.MetaInfo.Color : null,
            LogoFileId = x.MetaInfo != null ? x.MetaInfo.LogoFileId : null,
            DefaultStatusId = x.DefaultStatusId,
            DefaultStatusName = x.DefaultStatus == null ? null : x.DefaultStatus.Name,
            DefaultBoardIdentifier = x.ProjectBoards
                .Where(b => b.BoardType == BoardType.Default)
                .Select(b => b.Identifier)
                .FirstOrDefault(),
        };
    }

    public Task<List<string>> GetProjectMemberIds(int projectId, CancellationToken cancellationToken = default)
    {
        return Context.ProjectUsers
            .Where(member => member.ProjectId == projectId)
            .Select(member => member.UserId)
            .Distinct()
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<string> GenerateProjectKey(string projectName, int workspaceId, CancellationToken cancellationToken = default)
    {
        const int preferredKeyLength = 4;
        const string fallbackStem = "proj";

        var normalized = projectName.Trim().ToLowerInvariant();
        var stem = normalized.Length == 0 ? fallbackStem : normalized;
        var startingLength = Math.Min(preferredKeyLength, stem.Length);

        for (var length = startingLength; length <= stem.Length; length++)
        {
            var candidate = stem[..length];
            var isAvailable = await IsProjectKeyAvailable(candidate, workspaceId, cancellationToken);

            if (isAvailable)
            {
                return candidate;
            }
        }

        var numberedStem = stem[..startingLength];

        for (var suffix = 2; ; suffix++)
        {
            var candidate = $"{numberedStem}-{suffix}";
            var isAvailable = await IsProjectKeyAvailable(candidate, workspaceId, cancellationToken);

            if (isAvailable)
            {
                return candidate;
            }
        }
    }

    public async Task SetBrandingFile(int projectId, int workspaceId, string metaKey, string? fileId, CancellationToken cancellationToken = default)
    {
        using var connection = ConnectionFactory.StartConnection();

        var command = new CommandDefinition(
            SqlScripts.SetProjectBrandingFile,
            new { ProjectId = projectId, WorkspaceId = workspaceId, MetaKey = metaKey, FileId = fileId },
            cancellationToken: cancellationToken);

        await connection.ExecuteAsync(command);
    }
}
