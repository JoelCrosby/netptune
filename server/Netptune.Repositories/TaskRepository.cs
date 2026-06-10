using System.Linq.Expressions;

using Dapper;

using Microsoft.EntityFrameworkCore;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
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
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<ProjectTask?> GetAutomationTask(int id, CancellationToken cancellationToken = default)
    {
        return Entities
            .Include(task => task.ProjectTaskAppUsers)
            .Include(task => task.Project)
            .Include(task => task.Workspace)
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
        var statuses = filter.Statuses.Select(status => (int)status).ToArray();
        var assignees = filter.Assignees.Where(assignee => !string.IsNullOrWhiteSpace(assignee)).ToArray();
        var page = Math.Max(filter.Page ?? PaginationDefaults.DefaultPage, 1);
        var pageSize = Math.Clamp(filter.PageSize ?? PaginationDefaults.DefaultPageSize, 1, PaginationDefaults.MaxPageSize);
        var skip = (page - 1) * pageSize;

        using var connection = ConnectionFactory.StartConnection();

        var rows = await connection.QueryAsync<TaskViewRowMap>(new CommandDefinition($@"
            WITH filtered_tasks AS (
                SELECT pt.id
                     , pt.owner_id
                     , pt.name
                     , pt.description
                     , pt.status
                     , pt.project_scope_id
                     , pt.priority
                     , pt.estimate_type
                     , pt.estimate_value
                     , pt.project_id
                     , pt.sprint_id
                     , pt.workspace_id
                     , pt.created_at
                     , pt.updated_at
                     , w.slug AS workspace_key
                     , p.key AS project_key
                     , p.name AS project_name
                     , s.name AS sprint_name
                     , s.status AS sprint_status
                     , CASE
                           WHEN NULLIF(CONCAT_WS(' ', o.firstname, o.lastname), '') IS NULL
                               THEN o.user_name
                           ELSE CONCAT_WS(' ', o.firstname, o.lastname)
                       END AS owner_username
                     , o.picture_url AS owner_picture_url
                     , COUNT(*) OVER() AS total_count
                FROM project_tasks pt
                         INNER JOIN workspaces w ON pt.workspace_id = w.id
                         LEFT JOIN projects p ON pt.project_id = p.id
                         LEFT JOIN sprints s ON pt.sprint_id = s.id AND NOT s.is_deleted
                         INNER JOIN users o ON pt.owner_id = o.id
                WHERE w.slug = @workspaceKey
                  AND NOT pt.is_deleted
                  AND (@projectId IS NULL OR pt.project_id = @projectId)
                  AND (@sprintId IS NULL OR pt.sprint_id = @sprintId)
                  AND (@excludeSprintId IS NULL OR pt.sprint_id IS NULL OR pt.sprint_id != @excludeSprintId)
                  AND (@noSprint = FALSE OR pt.sprint_id IS NULL)
                  AND (CARDINALITY(@statuses) = 0 OR pt.status = ANY(@statuses))
                  AND (CARDINALITY(@assignees) = 0 OR EXISTS (
                      SELECT 1
                      FROM project_task_app_users ptau_filter
                      WHERE ptau_filter.project_task_id = pt.id
                        AND ptau_filter.user_id = ANY(@assignees)
                  ))
                  AND (CARDINALITY(@tags) = 0 OR EXISTS (
                      SELECT 1
                      FROM project_task_tags ptt_filter
                               INNER JOIN tags t_filter ON ptt_filter.tag_id = t_filter.id AND NOT t_filter.is_deleted
                      WHERE ptt_filter.project_task_id = pt.id
                        AND t_filter.name = ANY(@tags)
                  ))
                  AND (@search = '' OR (
                      LOWER(pt.name) LIKE @searchPattern
                      OR LOWER(p.key) LIKE @searchPattern
                      OR LOWER(p.name) LIKE @searchPattern
                      OR EXISTS (
                          SELECT 1
                          FROM project_task_tags ptt_search
                                   INNER JOIN tags t_search ON ptt_search.tag_id = t_search.id AND NOT t_search.is_deleted
                          WHERE ptt_search.project_task_id = pt.id
                            AND LOWER(t_search.name) LIKE @searchPattern
                      )
                  ))
                ORDER BY COALESCE(pt.updated_at, pt.created_at) DESC, pt.id DESC
                LIMIT @pageSize
                OFFSET @skip
            )
            SELECT ft.total_count
                 , ft.id AS task_id
                 , ft.owner_id
                 , ft.name AS task_name
                 , ft.description AS task_description
                 , ft.status AS task_status
                 , ft.project_scope_id
                 , ft.priority AS task_priority
                 , ft.estimate_type AS task_estimate_type
                 , ft.estimate_value AS task_estimate_value
                 , ft.project_id
                 , ft.sprint_id
                 , ft.sprint_name
                 , ft.sprint_status
                 , ft.workspace_id
                 , ft.workspace_key
                 , ft.created_at AS task_created_at
                 , ft.updated_at AS task_updated_at
                 , ft.owner_username
                 , ft.owner_picture_url
                 , ft.project_key
                 , ft.project_name
                 , t.name AS tag
                 , u.id AS assignee_id
                 , u.firstname AS assignee_firstname
                 , u.lastname AS assignee_lastname
                 , u.picture_url AS assignee_picture_url
            FROM filtered_tasks ft
                     LEFT JOIN project_task_tags ptt ON ft.id = ptt.project_task_id
                     LEFT JOIN tags t ON ptt.tag_id = t.id AND NOT t.is_deleted
                     LEFT JOIN project_task_app_users ptau ON ft.id = ptau.project_task_id
                     LEFT JOIN users u ON ptau.user_id = u.id
            ORDER BY COALESCE(ft.updated_at, ft.created_at) DESC, ft.id DESC, t.name, u.id;
        ", new
        {
            workspaceKey,
            projectId = filter.ProjectId,
            sprintId = filter.SprintId,
            excludeSprintId = filter.ExcludeSprintId,
            noSprint = filter.NoSprint ?? false,
            statuses,
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
                Status = row.Task_Status,
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
            Status = x.Status,
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

        var rows = await connection.QueryAsync<TasksViewRowMap>(new CommandDefinition(@"
                SELECT w.slug              AS workspace_key
                     , p.name              AS project_name
                     , p.key               AS project_key
                     , b.name              AS board_name
                     , b.identifier        AS board_identifier
                     , pt.id               AS task_id
                     , pt.name             AS task_name
                     , pt.description      AS task_description
                     , pt.project_scope_id AS project_scope_id
                     , pt.status           AS task_status
                     , pt.created_at       AS task_created_at
                     , pt.updated_at       AS task_updated_at
                     , ptibg.sort_order    AS task_sort_order
                     , bg.name             AS board_group_name
                     , bg.type             AS board_group_type
                     , bg.sort_order       AS board_group_sort_order
                     , u.firstname         AS assignee_firstname
                     , u.lastname          AS assignee_lastname
                     , u.email             AS assignee_email
                     , o.firstname         AS owner_firstname
                     , o.lastname          AS owner_lastname
                     , o.email             AS owner_email
                     , t.name              AS tag
                     , s.name              AS sprint_name
                     , s.status            AS sprint_status
                     , s.start_date        AS sprint_start_date
                     , s.end_date          AS sprint_end_date

                FROM workspaces w
                         LEFT JOIN projects p on p.workspace_id = w.id
                         LEFT JOIN boards b on b.project_id = p.id
                         LEFT JOIN board_groups bg ON b.id = bg.board_id AND NOT bg.is_deleted
                         LEFT JOIN project_task_in_board_groups ptibg on bg.id = ptibg.board_group_id
                         LEFT JOIN project_tasks pt on pt.id = ptibg.project_task_id
                            AND NOT pt.is_deleted
                            AND pt.id IN (
                                SELECT pt_limit.id
                                FROM project_tasks pt_limit
                                WHERE pt_limit.workspace_id = w.id
                                  AND NOT pt_limit.is_deleted
                                ORDER BY COALESCE(pt_limit.updated_at, pt_limit.created_at) DESC, pt_limit.id DESC
                                LIMIT @take
                            )
                         INNER JOIN users o on pt.owner_id = o.id
                         LEFT JOIN project_task_tags ptt on pt.id = ptt.project_task_id
                         LEFT JOIN tags t on ptt.tag_id = t.id AND NOT t.is_deleted
                         LEFT JOIN project_task_app_users ptau on pt.id = ptau.project_task_id
                         LEFT JOIN users u on ptau.user_id = u.id
                         LEFT JOIN sprints s on pt.sprint_id = s.id AND NOT s.is_deleted

                WHERE w.slug = @workspaceKey

                ORDER BY p.id, b.identifier, bg.sort_order, ptibg.sort_order;
            ", new
        {
            workspaceKey,
            take = PaginationDefaults.MaxExportRows,
        }, cancellationToken: cancellationToken));

        return RowsToExportList(rows);
    }

    public async Task<List<ExportTaskViewModel>> GetBoardExportTasksAsync(string workspaceKey, string boardIdentifier, CancellationToken cancellationToken = default)
    {
        using var connection = ConnectionFactory.StartConnection();

        var rows = await connection.QueryAsync<TasksViewRowMap>(new CommandDefinition(@"
                SELECT w.slug              AS workspace_key
                     , p.name              AS project_name
                     , p.key               AS project_key
                     , b.name              AS board_name
                     , b.identifier        AS board_identifier
                     , pt.id               AS task_id
                     , pt.name             AS task_name
                     , pt.description      AS task_description
                     , pt.project_scope_id AS project_scope_id
                     , pt.status           AS task_status
                     , pt.created_at       AS task_created_at
                     , pt.updated_at       AS task_updated_at
                     , ptibg.sort_order    AS task_sort_order
                     , bg.name             AS board_group_name
                     , bg.type             AS board_group_type
                     , bg.sort_order       AS board_group_sort_order
                     , u.firstname         AS assignee_firstname
                     , u.lastname          AS assignee_lastname
                     , u.email             AS assignee_email
                     , o.firstname         AS owner_firstname
                     , o.lastname          AS owner_lastname
                     , o.email             AS owner_email
                     , t.name              AS tag
                     , s.name              AS sprint_name
                     , s.status            AS sprint_status
                     , s.start_date        AS sprint_start_date
                     , s.end_date          AS sprint_end_date

                FROM workspaces w
                         LEFT JOIN projects p on p.workspace_id = w.id
                         LEFT JOIN boards b on b.project_id = p.id AND b.identifier = @boardIdentifier
                         LEFT JOIN board_groups bg ON b.id = bg.board_id AND NOT bg.is_deleted
                         LEFT JOIN project_task_in_board_groups ptibg on bg.id = ptibg.board_group_id
                         LEFT JOIN project_tasks pt on pt.id = ptibg.project_task_id
                            AND NOT pt.is_deleted
                            AND pt.id IN (
                                SELECT pt_limit.id
                                FROM boards b_limit
                                         INNER JOIN board_groups bg_limit ON b_limit.id = bg_limit.board_id AND NOT bg_limit.is_deleted
                                         INNER JOIN project_task_in_board_groups ptibg_limit ON bg_limit.id = ptibg_limit.board_group_id
                                         INNER JOIN project_tasks pt_limit ON pt_limit.id = ptibg_limit.project_task_id AND NOT pt_limit.is_deleted
                                WHERE b_limit.identifier = @boardIdentifier
                                ORDER BY bg_limit.sort_order, ptibg_limit.sort_order, pt_limit.id
                                LIMIT @take
                            )
                         INNER JOIN users o on pt.owner_id = o.id
                         LEFT JOIN project_task_tags ptt on pt.id = ptt.project_task_id
                         LEFT JOIN tags t on ptt.tag_id = t.id AND NOT t.is_deleted
                         LEFT JOIN project_task_app_users ptau on pt.id = ptau.project_task_id
                         LEFT JOIN users u on ptau.user_id = u.id
                         LEFT JOIN sprints s on pt.sprint_id = s.id AND NOT s.is_deleted

                WHERE w.slug = @workspaceKey

                ORDER BY bg.sort_order, ptibg.sort_order, t.name, u.id;;
            ", new
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
                Status = row.Task_Status.ToString(),
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

        var results = await connection.QueryAsync<int>(new CommandDefinition(@"
                SELECT pt.id
                FROM boards b

                INNER JOIN board_groups bg ON b.id = bg.board_id AND NOT bg.is_deleted
                INNER JOIN project_task_in_board_groups ptibg on bg.id = ptibg.board_group_id
                INNER JOIN project_tasks pt on pt.id = ptibg.project_task_id AND NOT pt.is_deleted

                WHERE b.identifier = @boardIdentifier
            ", new { boardIdentifier }, cancellationToken: cancellationToken));

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

    public Task<int> UpdateTaskStatus(int id, ProjectTaskStatus status, CancellationToken cancellationToken = default)
    {
        return Entities
            .Where(task => task.Id == id && !task.IsDeleted)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(task => task.Status, status)
                .SetProperty(task => task.UpdatedAt, DateTime.UtcNow), cancellationToken);
    }

    public Task<int> UpdateTaskStatuses(IEnumerable<int> ids, ProjectTaskStatus status, CancellationToken cancellationToken = default)
    {
        var idList = ids.ToList();

        if (idList.Count == 0) return Task.FromResult(0);

        return Entities
            .Where(task => idList.Contains(task.Id) && !task.IsDeleted)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(task => task.Status, status)
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

    private sealed class TaskViewRowMap
    {
        public int Total_Count { get; init; }

        public int Task_Id { get; init; }

        public string Owner_Id { get; init; } = null!;

        public string Task_Name { get; init; } = null!;

        public string? Task_Description { get; init; }

        public ProjectTaskStatus Task_Status { get; init; }

        public int Project_Scope_Id { get; init; }

        public TaskPriority? Task_Priority { get; init; }

        public EstimateType? Task_Estimate_Type { get; init; }

        public decimal? Task_Estimate_Value { get; init; }

        public int? Project_Id { get; init; }

        public int? Sprint_Id { get; init; }

        public string? Sprint_Name { get; init; }

        public SprintStatus? Sprint_Status { get; init; }

        public int Workspace_Id { get; init; }

        public string Workspace_Key { get; init; } = null!;

        public DateTime Task_Created_At { get; init; }

        public DateTime? Task_Updated_At { get; init; }

        public string Owner_Username { get; init; } = null!;

        public string? Owner_Picture_Url { get; init; }

        public string? Project_Key { get; init; }

        public string? Project_Name { get; init; }

        public string? Tag { get; init; }

        public string? Assignee_Id { get; init; }

        public string? Assignee_Firstname { get; init; }

        public string? Assignee_Lastname { get; init; }

        public string? Assignee_Picture_Url { get; init; }
    }
}
