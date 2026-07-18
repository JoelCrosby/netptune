using System.Linq.Expressions;

using Microsoft.EntityFrameworkCore;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Models.Sprints;
using Netptune.Core.Repositories;
using Netptune.Core.Repositories.Common;
using Netptune.Core.Requests;
using Netptune.Core.ViewModels.ProjectTasks;
using Netptune.Core.ViewModels.Sprints;
using Netptune.Core.ViewModels.Users;
using Netptune.Entities.Contexts;
using Netptune.Repositories.Common;

namespace Netptune.Repositories;

public class SprintRepository : WorkspaceEntityRepository<DataContext, Sprint, int>, ISprintRepository
{
    public SprintRepository(DataContext context, IDbConnectionFactory connectionFactory)
        : base(context, connectionFactory)
    {
    }

    public Task<List<SprintViewModel>> GetSprintsAsync(
        string workspaceKey,
        int? projectId = null,
        IReadOnlyCollection<SprintStatus>? statuses = null,
        int? take = null,
        string? sortBy = null,
        string? sortDirection = null,
        CancellationToken cancellationToken = default)
    {
        var statusList = statuses?.ToArray() ?? [];

        var filtered = Entities
            .Where(sprint => sprint.Workspace.Slug == workspaceKey && !sprint.IsDeleted)
            .Where(sprint => !projectId.HasValue || sprint.ProjectId == projectId.Value)
            .Where(sprint => statusList.Length == 0 || statusList.Contains(sprint.Status))
            .AsNoTracking();

        var query = ApplySprintOrder(filtered, sortBy, sortDirection);

        var limit = Math.Clamp(take ?? PaginationDefaults.DefaultPageSize, 1, PaginationDefaults.MaxPageSize);
        query = query.Take(limit);

        return query
            .Select(SprintToViewModel())
            .ToListAsync(cancellationToken);
    }

    public Task<List<SprintViewModel>> GetSprintViewModels(IEnumerable<int> sprintIds, CancellationToken cancellationToken = default)
    {
        var idList = sprintIds.ToList();

        if (idList.Count == 0)
        {
            return Task.FromResult(new List<SprintViewModel>());
        }

        return Entities
            .Where(sprint => idList.Contains(sprint.Id) && !sprint.IsDeleted)
            .AsNoTracking()
            .Select(SprintToViewModel())
            .ToListAsync(cancellationToken);
    }

    public Task<List<SprintViewModel>> GetAllSprintViewModels(string workspaceKey, CancellationToken cancellationToken = default)
    {
        return Entities
            .Where(sprint => sprint.Workspace.Slug == workspaceKey && !sprint.IsDeleted)
            .AsNoTracking()
            .Select(SprintToViewModel())
            .ToListAsync(cancellationToken);
    }

    private static IQueryable<Sprint> ApplySprintOrder(IQueryable<Sprint> query, string? sortBy, string? sortDirection)
    {
        var descending = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);

        IOrderedQueryable<Sprint> ordered = sortBy?.Trim() switch
        {
            "name" => descending
                ? query.OrderByDescending(sprint => sprint.Name)
                : query.OrderBy(sprint => sprint.Name),
            "status" => descending
                ? query.OrderByDescending(sprint => sprint.Status)
                : query.OrderBy(sprint => sprint.Status),
            "dates" => descending
                ? query.OrderByDescending(sprint => sprint.StartDate)
                : query.OrderBy(sprint => sprint.StartDate),
            "goal" => descending
                ? query.OrderByDescending(sprint => sprint.Goal)
                : query.OrderBy(sprint => sprint.Goal),
            "taskCount" => descending
                ? query.OrderByDescending(sprint => sprint.ProjectTasks.Count(task => !task.IsDeleted))
                : query.OrderBy(sprint => sprint.ProjectTasks.Count(task => !task.IsDeleted)),
            _ => query
                .OrderByDescending(sprint => sprint.Status == SprintStatus.Active)
                .ThenByDescending(sprint => sprint.StartDate),
        };

        return ordered.ThenBy(sprint => sprint.Id);
    }

    public Task<SprintDetailViewModel?> GetSprintDetailAsync(
        string workspaceKey,
        int sprintId,
        CancellationToken cancellationToken = default)
    {
        return Entities
            .Where(sprint => sprint.Id == sprintId && sprint.Workspace.Slug == workspaceKey && !sprint.IsDeleted)
            .AsNoTracking()
            .AsSplitQuery()
            .Select(SprintToDetailViewModel())
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<SprintDetailViewModel?> GetCurrentSprintForUserAsync(
        string workspaceKey,
        string userId,
        CancellationToken cancellationToken = default)
    {
        return Entities
            .Where(sprint => sprint.Workspace.Slug == workspaceKey && !sprint.IsDeleted)
            .Where(sprint => sprint.Status == SprintStatus.Active)
            .Where(sprint => sprint.ProjectTasks.Any(task =>
                !task.IsDeleted && task.ProjectTaskAppUsers.Any(assignee => assignee.UserId == userId)))
            .OrderByDescending(sprint => sprint.StartDate)
            .ThenByDescending(sprint => sprint.Id)
            .AsNoTracking()
            .AsSplitQuery()
            .Select(SprintToDetailViewModel())
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<Sprint?> GetSprintInWorkspaceAsync(
        string workspaceKey,
        int sprintId,
        bool isReadonly = false,
        CancellationToken cancellationToken = default)
    {
        var query = Entities
            .Include(sprint => sprint.Project)
            .Include(sprint => sprint.ProjectTasks)
            .ThenInclude(task => task.Status)
            .Include(sprint => sprint.Workspace)
            .Where(sprint => sprint.Id == sprintId && sprint.Workspace.Slug == workspaceKey && !sprint.IsDeleted)
            .AsSplitQuery();

        return query.IsReadonly(isReadonly).FirstOrDefaultAsync(cancellationToken);
    }

    public Task<SprintTaskAssignmentTarget?> GetTaskAssignmentTarget(
        string workspaceKey,
        int sprintId,
        CancellationToken cancellationToken = default)
    {
        return Entities
            .Where(sprint => sprint.Id == sprintId && sprint.Workspace.Slug == workspaceKey && !sprint.IsDeleted)
            .AsNoTracking()
            .Select(sprint => new SprintTaskAssignmentTarget(
                sprint.Id,
                sprint.Status,
                sprint.WorkspaceId,
                sprint.ProjectId))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<bool> HasActiveSprintAsync(
        int projectId,
        int? excludingSprintId = null,
        CancellationToken cancellationToken = default)
    {
        return Entities
            .AsNoTracking()
            .AnyAsync(sprint =>
                sprint.ProjectId == projectId &&
                sprint.Status == SprintStatus.Active &&
                !sprint.IsDeleted &&
                (!excludingSprintId.HasValue || sprint.Id != excludingSprintId.Value), cancellationToken);
    }

    private static Expression<Func<Sprint, SprintDetailViewModel>> SprintToDetailViewModel()
    {
        return sprint => new SprintDetailViewModel
        {
            Id = sprint.Id,
            Name = sprint.Name,
            Goal = sprint.Goal,
            Status = sprint.Status,
            StartDate = sprint.StartDate,
            StartedAt = sprint.StartedAt,
            EndDate = sprint.EndDate,
            CompletedAt = sprint.CompletedAt,
            ProjectId = sprint.ProjectId,
            ProjectName = sprint.Project.Name,
            ProjectKey = sprint.Project.Key,
            WorkspaceId = sprint.WorkspaceId,
            CreatedAt = sprint.CreatedAt,
            UpdatedAt = sprint.UpdatedAt,
            TaskCount = sprint.ProjectTasks.Count(task => !task.IsDeleted),
            NewTaskCount = sprint.ProjectTasks.Count(task => !task.IsDeleted && task.Status.Category == StatusCategory.Todo),
            ActiveTaskCount = sprint.ProjectTasks.Count(task => !task.IsDeleted && task.Status.Category == StatusCategory.Active),
            DoneTaskCount = sprint.ProjectTasks.Count(task => !task.IsDeleted && task.Status.Category == StatusCategory.Done),
            EstimateType = sprint.ProjectTasks
                .Where(task => !task.IsDeleted && task.EstimateType.HasValue)
                .Select(task => task.EstimateType)
                .FirstOrDefault(),
            TotalEstimateValue = sprint.ProjectTasks
                .Where(task => !task.IsDeleted)
                .Sum(task => task.EstimateValue),
            Tasks = sprint.ProjectTasks
                .Where(task => !task.IsDeleted)
                .OrderByDescending(task => task.UpdatedAt)
                .AsQueryable()
                .Select(TaskToViewModel())
                .ToList(),
        };
    }

    private static Expression<Func<Sprint, SprintViewModel>> SprintToViewModel()
    {
        return sprint => new SprintViewModel
        {
            Id = sprint.Id,
            Name = sprint.Name,
            Goal = sprint.Goal,
            Status = sprint.Status,
            StartDate = sprint.StartDate,
            StartedAt = sprint.StartedAt,
            EndDate = sprint.EndDate,
            CompletedAt = sprint.CompletedAt,
            ProjectId = sprint.ProjectId,
            ProjectName = sprint.Project.Name,
            ProjectKey = sprint.Project.Key,
            WorkspaceId = sprint.WorkspaceId,
            CreatedAt = sprint.CreatedAt,
            UpdatedAt = sprint.UpdatedAt,
            TaskCount = sprint.ProjectTasks.Count(task => !task.IsDeleted),
        };
    }

    private static Expression<Func<ProjectTask, TaskViewModel>> TaskToViewModel()
    {
        return task => new TaskViewModel
        {
            Id = task.Id,
            OwnerId = task.OwnerId!,
            Name = task.Name,
            Description = task.Description,
            StatusId = task.StatusId,
            StatusName = task.Status.Name,
            StatusKey = task.Status.Key,
            StatusColor = task.Status.Color,
            StatusCategory = task.Status.Category,
            ProjectScopeId = task.ProjectScopeId,
            SystemId = task.Project == null
                ? task.ProjectScopeId.ToString()
                : task.Project.Key + "-" + task.ProjectScopeId.ToString(),
            Priority = task.Priority,
            EstimateType = task.EstimateType,
            EstimateValue = task.EstimateValue,
            DueDate = task.DueDate,
            ProjectId = task.ProjectId,
            SprintId = task.SprintId,
            SprintName = task.Sprint == null ? null : task.Sprint.Name,
            SprintStatus = task.Sprint == null ? null : task.Sprint.Status,
            WorkspaceId = task.WorkspaceId,
            WorkspaceKey = task.Workspace.Slug,
            CreatedAt = task.CreatedAt,
            UpdatedAt = task.UpdatedAt,
            OwnerUsername = string.IsNullOrEmpty(task.Owner!.Firstname) && string.IsNullOrEmpty(task.Owner.Lastname)
                ? task.Owner.UserName!
                : task.Owner.Firstname + " " + task.Owner.Lastname,
            OwnerPictureUrl = task.Owner.PictureUrl,
            ProjectName = task.Project == null ? string.Empty : task.Project.Name,
            Tags = task.Tags.Select(tag => tag.Name).OrderBy(name => name).ToList(),
            Assignees = task.ProjectTaskAppUsers.Select(user => new AssigneeViewModel
            {
                Id = user.User.Id,
                DisplayName = user.User.Firstname + " " + user.User.Lastname,
                PictureUrl = user.User.PictureUrl,
            }).ToList(),
        };
    }
}
