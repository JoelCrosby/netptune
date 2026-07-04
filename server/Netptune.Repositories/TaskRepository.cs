using System.Linq.Expressions;

using Dapper;

using Microsoft.EntityFrameworkCore;

using Netptune.Core.Entities;
using Netptune.Core.Relationships;
using Netptune.Core.Repositories;
using Netptune.Core.Repositories.Common;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.ProjectTasks;
using Netptune.Core.ViewModels.Users;
using Netptune.Entities.Contexts;
using Netptune.Repositories.Common;
using Netptune.Repositories.RowMaps;
using Netptune.Repositories.Sql;

namespace Netptune.Repositories;

public class TaskRepository : WorkspaceEntityRepository<DataContext, ProjectTask, int>, ITaskRepository
{
    public TaskRepository(DataContext context, IDbConnectionFactory connectionFactory)
        : base(context, connectionFactory)
    {
    }

    public override Task<ProjectTask?> GetAsync(int id, bool isReadonly = false, CancellationToken cancellationToken = default)
    {
        return Entities
            .Include(x => x.ProjectTaskAppUsers)
                .ThenInclude(x => x.User)
            .Include(x => x.Project)
            .Include(x => x.Sprint)
            .Include(x => x.Status)
            .Include(x => x.Owner)
            .Include(x => x.Workspace)
            .AsSplitQuery()
            .IsReadonly(isReadonly)
            .FirstOrDefaultAsync(EqualsPredicate(id), cancellationToken);
    }

    public Task<ProjectTask?> GetTaskForUpdate(int id, CancellationToken cancellationToken = default)
    {
        return Entities
            .Include(x => x.ProjectTaskAppUsers)
            .Include(x => x.Status)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<List<ProjectTask>> GetTasksForUpdate(IEnumerable<int> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids.ToList();

        return Entities
            .Include(x => x.ProjectTaskAppUsers)
            .Include(x => x.Status)
            .Where(x => idList.Contains(x.Id) && !x.IsDeleted)
            .ToListAsync(cancellationToken);
    }

    public Task<ProjectTask?> GetAutomationTask(int id, CancellationToken cancellationToken = default)
    {
        return Entities
            .Include(task => task.ProjectTaskAppUsers)
            .Include(task => task.Project)
            .Include(task => task.Workspace)
            .Include(task => task.Status)
            .AsNoTracking()
            .FirstOrDefaultAsync(task => task.Id == id && !task.IsDeleted, cancellationToken);
    }

    public Task<List<ProjectTask>> GetUnassignedAutomationCandidates(
        IReadOnlyCollection<int> workspaceIds,
        DateTime cutoff,
        CancellationToken cancellationToken = default)
    {
        return Entities
            .Include(task => task.ProjectTaskAppUsers)
            .Include(task => task.Project)
            .Include(task => task.Workspace)
            .Include(task => task.Status)
            .Where(task =>
                workspaceIds.Contains(task.WorkspaceId) &&
                !task.IsDeleted &&
                !task.ProjectTaskAppUsers.Any() &&
                (task.UpdatedAt ?? task.CreatedAt) <= cutoff)
            .AsNoTracking()
            .AsSplitQuery()
            .ToListAsync(cancellationToken);
    }

    public Task<TaskViewModel?> GetTaskViewModel(int taskId, CancellationToken cancellationToken = default)
    {
        return Entities
            .Where(x => x.Id == taskId)
            .AsNoTracking()
            .Select(TaskToViewModel())
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<int?> GetTaskInternalId(string systemId, string workspaceKey, CancellationToken cancellationToken = default)
    {
        var entity = GetTaskFromSystemId(systemId, workspaceKey, true);

        if (entity is null) return null;

        var task = await entity.Select(x => (int?)x.Id).FirstOrDefaultAsync(cancellationToken);

        return task;
    }

    public async Task<ProjectTask?> GetTask(string systemId, string workspaceKey, CancellationToken cancellationToken = default)
    {
        var entity = GetTaskFromSystemId(systemId, workspaceKey, true);

        if (entity is null) return null;

        return await entity.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<TaskViewModel?> GetTaskViewModel(string systemId, string workspaceKey, CancellationToken cancellationToken = default)
    {
        var entity = GetTaskFromSystemId(systemId, workspaceKey, true);

        if (entity is null) return null;

        return await entity
            .Where(e => !e.IsDeleted)
            .Select(TaskToViewModel())
            .FirstOrDefaultAsync(cancellationToken);
    }

    private IQueryable<ProjectTask>? GetTaskFromSystemId(string systemId, string workspaceKey, bool isReadonly = false)
    {
        var parts = systemId.Split("-");

        if (!int.TryParse(parts.LastOrDefault(), out var projectScopeId)) return null;

        var projectKey = parts[0];

        var queryable = Entities
            .Where(x =>
                x.Workspace.Slug == workspaceKey &&
                x.Project!.Key == projectKey &&
                x.ProjectScopeId == projectScopeId)
            .Include(x => x.ProjectTaskAppUsers).ThenInclude(x => x.User)
            .Include(x => x.Project)
            .Include(x => x.Owner)
            .Include(x => x.Workspace)
            .Include(x => x.Status)
            .Include(x => x.Tags)
            .AsSplitQuery();

        return isReadonly ? queryable.AsNoTracking() : queryable;
    }

    public async Task<PagedResponse<TaskViewModel>> GetTasksAsync(string workspaceKey, TaskFilter? filter = null, bool isReadonly = false, CancellationToken cancellationToken = default)
    {
        filter ??= new TaskFilter();

        var search = filter.Search?.Trim().ToLower() ?? string.Empty;
        var searchPattern = $"%{search}%";
        var tags = filter.Tags.Where(tag => !string.IsNullOrWhiteSpace(tag)).ToArray();
        var statusIds = filter.StatusIds;
        var assignees = filter.Assignees.Where(assignee => !string.IsNullOrWhiteSpace(assignee)).ToArray();
        var page = Math.Max(filter.Page ?? PaginationDefaults.DefaultPage, 1);
        var pageSize = Math.Clamp(filter.PageSize ?? PaginationDefaults.DefaultPageSize, 1, PaginationDefaults.MaxPageSize);
        var skip = (page - 1) * pageSize;
        var taskOrder = GetTaskOrderBy(filter);
        var rowOrder = GetTaskRowOrderBy(filter);

        var sql = SqlScripts.GetTasks
            .Replace("{taskOrder}", taskOrder)
            .Replace("{rowOrder}", rowOrder);

        using var connection = ConnectionFactory.StartConnection();

        var rows = await connection.QueryAsync<TaskViewRowMap>(new CommandDefinition(sql, new
        {
            workspaceKey,
            projectId = filter.ProjectId,
            sprintId = filter.SprintId,
            excludeSprintId = filter.ExcludeSprintId,
            noSprint = filter.NoSprint ?? false,
            statusIds,
            statusCategories = filter.StatusCategories,
            tags,
            assignees,
            search,
            searchPattern,
            pageSize,
            skip,
        }, cancellationToken: cancellationToken));

        var rowList = rows.ToList();
        var totalCount = rowList.FirstOrDefault()?.Total_Count ?? 0;

        return new PagedResponse<TaskViewModel>(RowsToTaskViewModels(rowList), page, pageSize, totalCount);
    }

    public async Task<List<TaskStatusBreakdownItem>> GetTaskStatusBreakdownAsync(string workspaceKey, CancellationToken cancellationToken = default)
    {
        using var connection = ConnectionFactory.StartConnection();

        var rows = await connection.QueryAsync<TaskStatusBreakdownItem>(new CommandDefinition(
            SqlScripts.GetTaskStatusBreakdown,
            new { workspaceKey },
            cancellationToken: cancellationToken));

        return rows.ToList();
    }

    private static string GetTaskOrderBy(TaskFilter filter)
    {
        var direction = GetSortDirection(filter);

        return GetTaskSortExpression(filter.SortBy) switch
        {
            null => "COALESCE(pt.updated_at, pt.created_at) DESC, pt.id DESC",
            var expression => $"{expression} {direction} NULLS LAST, pt.id {direction}",
        };
    }

    private static string GetTaskRowOrderBy(TaskFilter filter)
    {
        var direction = GetSortDirection(filter);

        return GetTaskRowSortExpression(filter.SortBy) switch
        {
            null => "COALESCE(ft.updated_at, ft.created_at) DESC, ft.id DESC",
            var expression => $"{expression} {direction} NULLS LAST, ft.id {direction}",
        };
    }

    private static string GetSortDirection(TaskFilter filter)
    {
        return string.Equals(filter.SortDirection, "asc", StringComparison.OrdinalIgnoreCase)
            ? "ASC"
            : "DESC";
    }

    private static string? GetTaskSortExpression(string? sortBy)
    {
        return sortBy?.Trim() switch
        {
            "id" => "pt.id",
            "name" => "pt.name",
            "systemId" => "CONCAT_WS('-', p.key, pt.project_scope_id::text)",
            "projectScopeId" => "pt.project_scope_id",
            "sprint" => "s.name",
            "status" => "st.name",
            "createdAt" => "pt.created_at",
            "updatedAt" => "pt.updated_at",
            "assignees" => "assignee_count",
            _ => null,
        };
    }

    private static string? GetTaskRowSortExpression(string? sortBy)
    {
        return sortBy?.Trim() switch
        {
            "id" => "ft.id",
            "name" => "ft.name",
            "systemId" => "CONCAT_WS('-', ft.project_key, ft.project_scope_id::text)",
            "projectScopeId" => "ft.project_scope_id",
            "sprint" => "ft.sprint_name",
            "status" => "ft.status_name",
            "createdAt" => "ft.created_at",
            "updatedAt" => "ft.updated_at",
            "assignees" => "ft.assignee_count",
            _ => null,
        };
    }

    private static List<TaskViewModel> RowsToTaskViewModels(IEnumerable<TaskViewRowMap> rows)
    {
        return rows.Aggregate(new List<TaskViewModel>(200), (result, row) =>
        {
            var lastTask = result.LastOrDefault();

            if (lastTask?.Id == row.Task_Id)
            {
                AddTag(lastTask, row.Tag);
                AddAssignee(lastTask, row);

                return result;
            }

            var task = new TaskViewModel
            {
                Id = row.Task_Id,
                OwnerId = row.Owner_Id,
                Name = row.Task_Name,
                Description = row.Task_Description,
                StatusId = row.Task_Status_Id,
                StatusName = row.Task_Status_Name,
                StatusKey = row.Task_Status_Key,
                StatusColor = row.Task_Status_Color,
                StatusCategory = row.Task_Status_Category,
                ProjectScopeId = row.Project_Scope_Id,
                SystemId = row.Project_Key is null
                    ? row.Project_Scope_Id.ToString()
                    : $"{row.Project_Key}-{row.Project_Scope_Id}",
                Priority = row.Task_Priority,
                EstimateType = row.Task_Estimate_Type,
                EstimateValue = row.Task_Estimate_Value,
                ProjectId = row.Project_Id,
                SprintId = row.Sprint_Id,
                SprintName = row.Sprint_Name,
                SprintStatus = row.Sprint_Status,
                WorkspaceId = row.Workspace_Id,
                WorkspaceKey = row.Workspace_Key,
                CreatedAt = row.Task_Created_At,
                UpdatedAt = row.Task_Updated_At,
                OwnerUsername = row.Owner_Username,
                OwnerPictureUrl = row.Owner_Picture_Url,
                ProjectName = row.Project_Name ?? string.Empty,
                Tags = [],
                Assignees = [],
            };

            AddTag(task, row.Tag);
            AddAssignee(task, row);

            result.Add(task);

            return result;
        });
    }

    private static void AddTag(TaskViewModel task, string? tag)
    {
        if (tag is not null && !task.Tags.Contains(tag))
        {
            task.Tags.Add(tag);
        }
    }

    private static void AddAssignee(TaskViewModel task, TaskViewRowMap row)
    {
        if (row.Assignee_Id is null || task.Assignees.Any(assignee => assignee.Id == row.Assignee_Id))
        {
            return;
        }

        task.Assignees.Add(new AssigneeViewModel
        {
            Id = row.Assignee_Id,
            DisplayName = $"{row.Assignee_Firstname} {row.Assignee_Lastname}",
            PictureUrl = row.Assignee_Picture_Url,
        });
    }

    private static Expression<Func<ProjectTask, TaskViewModel>> TaskToViewModel()
    {
        return x => new TaskViewModel
        {
            Id = x.Id,
            OwnerId = x.OwnerId!,
            Name = x.Name,
            Description = x.Description,
            StatusId = x.StatusId,
            StatusName = x.Status.Name,
            StatusKey = x.Status.Key,
            StatusColor = x.Status.Color,
            StatusCategory = x.Status.Category,
            ProjectScopeId = x.ProjectScopeId,
            SystemId = x.Project == null
                ? x.ProjectScopeId.ToString()
                : x.Project.Key + "-" + x.ProjectScopeId.ToString(),
            Priority = x.Priority,
            EstimateType = x.EstimateType,
            EstimateValue = x.EstimateValue,
            ProjectId = x.ProjectId,
            SprintId = x.SprintId,
            SprintName = x.Sprint == null ? null : x.Sprint.Name,
            SprintStatus = x.Sprint == null ? null : x.Sprint.Status,
            WorkspaceId = x.WorkspaceId,
            WorkspaceKey = x.Workspace.Slug,
            CreatedAt = x.CreatedAt,
            UpdatedAt = x.UpdatedAt,
            OwnerUsername = string.IsNullOrEmpty(x.Owner!.Firstname) && string.IsNullOrEmpty(x.Owner.Lastname)
                ? x.Owner.UserName!
                : x.Owner.Firstname + " " + x.Owner.Lastname,
            OwnerPictureUrl = x.Owner.PictureUrl,
            ProjectName = x.Project == null ? string.Empty : x.Project.Name,
            Tags = x.Tags.Select(t => t.Name).OrderBy(n => n).ToList(),
            Assignees = x.ProjectTaskAppUsers.Select(u => new AssigneeViewModel
            {
                Id = u.User.Id,
                DisplayName = u.User.Firstname + " " + u.User.Lastname,
                PictureUrl = u.User.PictureUrl,
            }).ToList(),
        };
    }

    public async Task<List<ExportTaskViewModel>> GetExportTasksAsync(string workspaceKey, CancellationToken cancellationToken = default)
    {
        using var connection = ConnectionFactory.StartConnection();

        var rows = await connection.QueryAsync<TasksViewRowMap>(new CommandDefinition(SqlScripts.GetExportTasks, new
        {
            workspaceKey,
            take = PaginationDefaults.MaxExportRows,
        }, cancellationToken: cancellationToken));

        return RowsToExportList(rows);
    }

    public async Task<List<ExportTaskViewModel>> GetBoardExportTasksAsync(string workspaceKey, string boardIdentifier, CancellationToken cancellationToken = default)
    {
        using var connection = ConnectionFactory.StartConnection();

        var rows = await connection.QueryAsync<TasksViewRowMap>(new CommandDefinition(SqlScripts.GetBoardExportTasks, new
        {
            workspaceKey,
            boardIdentifier,
            take = PaginationDefaults.MaxExportRows,
        }, cancellationToken: cancellationToken));

        return RowsToExportList(rows);
    }

    private static readonly HashSet<string> Empty = new();

    private static List<ExportTaskViewModel> RowsToExportList(IEnumerable<TasksViewRowMap> rows)
    {
        return rows.Aggregate(new List<ExportTaskViewModel>(200), (result, row) =>
        {
            var lastTask = result.LastOrDefault();
            var lastTag = lastTask?.Tags.LastOrDefault();
            var lastAssignee = lastTask?.Assignees.FirstOrDefault();
            var systemId = $"{row.Project_Key}-{row.Project_Scope_Id}";

            if (lastTask?.SystemId is not null && systemId == lastTask.SystemId)
            {
                if (lastTag != row.Tag && row.Tag is not null)
                {
                    lastTask.Tags.Add(row.Tag);
                }
                else if (lastAssignee != row.Assignee_Email && row.Assignee_Email is not null)
                {
                    lastTask.Assignees.Add(row.Assignee_Email);
                }

                return result;
            }

            static HashSet<string> Set(string? initialValue)
            {
                return initialValue is null ? Empty : new HashSet<string> { initialValue };
            }

            result.Add(new ExportTaskViewModel
            {
                Name = row.Task_Name,
                Description = row.Task_Description,
                SystemId = systemId,
                Status = row.Task_Status,
                SortOrder = row.Task_Sort_Order,
                Board = row.Board_Identifier,
                CreatedAt = row.Task_Created_At,
                UpdatedAt = row.Task_Updated_At,
                Assignees = Set(row.Assignee_Email),
                Owner = row.Owner_Email,
                Project = row.Project_Name,
                Group = row.Board_Group_Name,
                Sprint = row.Sprint_Name,
                SprintStatus = row.Sprint_Status?.ToString(),
                SprintStartDate = row.Sprint_Start_Date,
                SprintEndDate = row.Sprint_End_Date,
                Tags = Set(row.Tag),
            });

            return result;
        });
    }

    public async Task<List<int>> GetTaskIdsInBoard(string boardIdentifier, CancellationToken cancellationToken = default)
    {
        using var connection = ConnectionFactory.StartConnection();

        var results = await connection.QueryAsync<int>(new CommandDefinition(
            SqlScripts.GetTaskIdsInBoard, new { boardIdentifier }, cancellationToken: cancellationToken));

        return results.ToList();
    }

    public async Task<int?> GetNextScopeId(int projectId, int increment = 0, CancellationToken cancellationToken = default)
    {
        var taskCount = await Entities
            .Where(x => x.ProjectId == projectId)
            .OrderByDescending(x => x.ProjectScopeId)
            .Select(x => x.ProjectScopeId)
            .FirstOrDefaultAsync(cancellationToken);

        return taskCount + 1 + increment;
    }

    public Task<int> UpdateTaskStatus(int id, int statusId, CancellationToken cancellationToken = default)
    {
        return Entities
            .Where(task => task.Id == id && !task.IsDeleted)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(task => task.StatusId, statusId)
                .SetProperty(task => task.UpdatedAt, DateTime.UtcNow), cancellationToken);
    }

    public Task<int> UpdateTaskStatuses(IEnumerable<int> ids, int statusId, CancellationToken cancellationToken = default)
    {
        var idList = ids.ToList();

        if (idList.Count == 0) return Task.FromResult(0);

        return Entities
            .Where(task => idList.Contains(task.Id) && !task.IsDeleted)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(task => task.StatusId, statusId)
                .SetProperty(task => task.UpdatedAt, DateTime.UtcNow), cancellationToken);
    }

    public Task<List<int>> GetValidTaskIdsInWorkspace(IEnumerable<int> taskIds, int workspaceId, CancellationToken cancellationToken = default)
    {
        var taskIdList = taskIds.ToList();

        return Entities
            .AsNoTracking()
            .Where(task => taskIdList.Contains(task.Id))
            .Where(task => task.WorkspaceId == workspaceId && !task.IsDeleted)
            .Select(task => task.Id)
            .ToListAsync(cancellationToken);
    }

    public Task<List<int>> GetValidTaskIdsInProject(IEnumerable<int> taskIds, int workspaceId, int projectId, CancellationToken cancellationToken = default)
    {
        var taskIdList = taskIds.ToList();

        return Entities
            .AsNoTracking()
            .Where(task => taskIdList.Contains(task.Id))
            .Where(task => task.WorkspaceId == workspaceId && task.ProjectId == projectId && !task.IsDeleted)
            .Select(task => task.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task AssignTasksToSprint(IEnumerable<int> taskIds, int sprintId, CancellationToken cancellationToken = default)
    {
        var taskIdList = taskIds.ToList();

        if (taskIdList.Count == 0)
        {
            return;
        }

        await Entities
            .Where(task => taskIdList.Contains(task.Id))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(task => task.SprintId, sprintId)
                .SetProperty(task => task.UpdatedAt, DateTime.UtcNow), cancellationToken);
    }

    public async Task AssignTasksToUser(IEnumerable<int> taskIds, string assigneeId, CancellationToken cancellationToken = default)
    {
        var taskIdList = taskIds.ToList();

        if (taskIdList.Count == 0)
        {
            return;
        }

        await Entities
            .Where(task => taskIdList.Contains(task.Id))
            .ExecuteUpdateAsync(setters => setters.SetProperty(task => task.UpdatedAt, DateTime.UtcNow), cancellationToken);

        var existingTaskIds = await Context.ProjectTaskAppUsers
            .AsNoTracking()
            .Where(assignment => assignment.UserId == assigneeId && taskIdList.Contains(assignment.ProjectTaskId))
            .Select(assignment => assignment.ProjectTaskId)
            .ToListAsync(cancellationToken);

        var missingAssignments = taskIdList
            .Except(existingTaskIds)
            .Select(taskId => new ProjectTaskAppUser
            {
                ProjectTaskId = taskId,
                UserId = assigneeId,
            });

        await Context.ProjectTaskAppUsers.AddRangeAsync(missingAssignments, cancellationToken);
    }
}
