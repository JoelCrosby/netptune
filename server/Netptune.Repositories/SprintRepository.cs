using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Repositories;
using Netptune.Core.Repositories.Common;
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
        SprintStatus? status = null,
        int? take = null,
        CancellationToken cancellationToken = default)
    {
        var query = Entities
            .Where(sprint => sprint.Workspace.Slug == workspaceKey && !sprint.IsDeleted)
            .Where(sprint => !projectId.HasValue || sprint.ProjectId == projectId.Value)
            .Where(sprint => !status.HasValue || sprint.Status == status.Value)
            .OrderByDescending(sprint => sprint.Status == SprintStatus.Active)
            .ThenByDescending(sprint => sprint.StartDate)
            .AsNoTracking();

        if (take is > 0)
        {
            query = query.Take(Math.Min(take.Value, 100));
        }

        return query
            .Select(SprintToViewModel())
            .ToListAsync(cancellationToken);
    }

    public Task<SprintDetailViewModel?> GetSprintDetailAsync(
        string workspaceKey,
        int sprintId,
        CancellationToken cancellationToken = default)
    {
        return Entities
            .Where(sprint => sprint.Id == sprintId && sprint.Workspace.Slug == workspaceKey && !sprint.IsDeleted)
            .AsNoTracking()
            .Select(sprint => new SprintDetailViewModel
            {
                Id = sprint.Id,
                Name = sprint.Name,
                Goal = sprint.Goal,
                Status = sprint.Status,
                StartDate = sprint.StartDate,
                EndDate = sprint.EndDate,
                CompletedAt = sprint.CompletedAt,
                ProjectId = sprint.ProjectId,
                ProjectName = sprint.Project.Name,
                ProjectKey = sprint.Project.Key,
                WorkspaceId = sprint.WorkspaceId,
                CreatedAt = sprint.CreatedAt,
                UpdatedAt = sprint.UpdatedAt,
                TaskCount = sprint.ProjectTasks.Count(task => !task.IsDeleted),
                NewTaskCount = sprint.ProjectTasks.Count(task => !task.IsDeleted && task.Status == ProjectTaskStatus.New),
                ActiveTaskCount = sprint.ProjectTasks.Count(task => !task.IsDeleted && task.Status == ProjectTaskStatus.InProgress),
                DoneTaskCount = sprint.ProjectTasks.Count(task => !task.IsDeleted && task.Status == ProjectTaskStatus.Complete),
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
            })
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
            .Include(sprint => sprint.Workspace)
            .Where(sprint => sprint.Id == sprintId && sprint.Workspace.Slug == workspaceKey && !sprint.IsDeleted)
            .AsSplitQuery();

        return query.IsReadonly(isReadonly).FirstOrDefaultAsync(cancellationToken);
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

    private static Expression<Func<Sprint, SprintViewModel>> SprintToViewModel()
    {
        return sprint => new SprintViewModel
        {
            Id = sprint.Id,
            Name = sprint.Name,
            Goal = sprint.Goal,
            Status = sprint.Status,
            StartDate = sprint.StartDate,
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
            Status = task.Status,
            ProjectScopeId = task.ProjectScopeId,
            SystemId = task.Project == null
                ? task.ProjectScopeId.ToString()
                : task.Project.Key + "-" + task.ProjectScopeId.ToString(),
            Priority = task.Priority,
            EstimateType = task.EstimateType,
            EstimateValue = task.EstimateValue,
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
