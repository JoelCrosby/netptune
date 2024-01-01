using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Dapper;

using Microsoft.EntityFrameworkCore;

using Netptune.Core.Entities;
using Netptune.Core.Models.Activity;
using Netptune.Core.Repositories;
using Netptune.Core.Repositories.Common;
using Netptune.Core.ViewModels.ProjectTasks;
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

    public override Task<ProjectTask?> GetAsync(int id, bool isReadonly = false)
    {
        return Entities
            .Include(x => x.ProjectTaskAppUsers)
                .ThenInclude(x => x.User)
            .Include(x => x.Project)
            .Include(x => x.Owner)
            .Include(x => x.Workspace)
            .AsSplitQuery()
            .IsReadonly(isReadonly)
            .FirstOrDefaultAsync(EqualsPredicate(id));
    }

    public Task<TaskViewModel?> GetTaskViewModel(int taskId)
    {
        return Entities
            .Where(x => x.Id == taskId)
            .OrderByDescending(x => x.UpdatedAt)
            .Include(x => x.ProjectTaskAppUsers)
                .ThenInclude(x => x.User)
            .Include(x => x.Project)
            .Include(x => x.Owner)
            .Include(x => x.Workspace)
            .Include(x => x.Tags)
            .AsSplitQuery()
            .AsNoTracking()
            .Select(task => task.ToViewModel())
            .FirstOrDefaultAsync();
    }

    public async Task<int?> GetTaskInternalId(string systemId, string workspaceKey)
    {
        var entity = await GetTaskFromSystemId(systemId, workspaceKey, true);

        if (entity is null) return null;

        var task = await entity.FirstOrDefaultAsync();

        return task?.Id;
    }

    public async Task<ProjectTask?> GetTask(string systemId, string workspaceKey)
    {
        var entity = await GetTaskFromSystemId(systemId, workspaceKey, true);

        if (entity is null) return null;

        return await entity.FirstOrDefaultAsync();
    }

    public async Task<TaskViewModel?> GetTaskViewModel(string systemId, string workspaceKey)
    {
        var entity = await GetTaskFromSystemId(systemId, workspaceKey, true);

        if (entity is null) return null;

        return await entity
            .Select(task => task.ToViewModel())
            .FirstOrDefaultAsync();
    }

    private async Task<IQueryable<ProjectTask>?> GetTaskFromSystemId(string systemId, string workspaceKey, bool isReadonly = false)
    {
        var parts = systemId.Split("-");

        var hasProjectId = int.TryParse(parts.LastOrDefault(), out var projectScopeId);

        if (!hasProjectId) return null;

        var projectKey = parts[0];

        var workspaceIds = await Context.Workspaces
            .AsNoTracking()
            .Where(x => x.Slug == workspaceKey)
            .Select(x => x.Id)
            .Take(1)
            .ToListAsync();

        if (!workspaceIds.Any()) return null;

        var workspaceId = workspaceIds.FirstOrDefault();

        var projectIds = await Context.Projects
            .AsNoTracking()
            .Where(x => x.Key == projectKey && x.WorkspaceId == workspaceId)
            .Select(x => x.Id)
            .Take(1)
            .ToListAsync();

        if (!projectIds.Any()) return null;

        var projectId = projectIds.FirstOrDefault();

        var queryable = Entities
            .Where(x => x.ProjectScopeId == projectScopeId && x.WorkspaceId == workspaceId && x.ProjectId == projectId)
            .OrderByDescending(x => x.UpdatedAt)
            .Include(x => x.ProjectTaskAppUsers)
                .ThenInclude(x => x.User)
            .Include(x => x.Project)
            .Include(x => x.Owner)
            .Include(x => x.Workspace)
            .Include(x => x.Tags)
            .AsSplitQuery();

        return isReadonly ? queryable.AsNoTracking() : queryable;
    }

    public Task<List<TaskViewModel>> GetTasksAsync(string workspaceKey, bool isReadonly = false)
    {
        return Entities
            .Where(x => x.Workspace.Slug == workspaceKey && !x.IsDeleted)
            .OrderByDescending(x => x.UpdatedAt)
            .Include(x => x.ProjectTaskAppUsers)
                .ThenInclude(x => x.User)
            .Include(x => x.Project)
            .Include(x => x.Owner)
            .Include(x => x.Workspace)
            .AsSplitQuery()
            .Select(task => task.ToViewModel())
            .ToReadonlyListAsync(isReadonly);
    }

    public async Task<List<ExportTaskViewModel>> GetExportTasksAsync(string workspaceKey)
    {
        using var connection = ConnectionFactory.StartConnection();

        var rows = await connection.QueryAsync<TasksViewRowMap>(@"
                SELECT w.slug              AS workspace_key
                     , p.name              AS project_name
                     , p.key               AS project_key
                     , b.name              AS board_name
                     , b.identifier        AS board_identifier
                     , pt.id               AS task_id
                     , pt.name             AS task_name
                     , pt.description      AS task_description
                     , pt.is_flagged       AS task_is_flagged
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

                FROM workspaces w
                         LEFT JOIN projects p on p.workspace_id = w.id
                         LEFT JOIN boards b on b.project_id = p.id
                         LEFT JOIN board_groups bg ON b.id = bg.board_id AND NOT bg.is_deleted
                         LEFT JOIN project_task_in_board_groups ptibg on bg.id = ptibg.board_group_id
                         LEFT JOIN project_tasks pt on pt.id = ptibg.project_task_id AND NOT pt.is_deleted
                         INNER JOIN users o on pt.owner_id = o.id
                         LEFT JOIN project_task_tags ptt on pt.id = ptt.project_task_id
                         LEFT JOIN tags t on ptt.tag_id = t.id AND NOT t.is_deleted
                         LEFT JOIN project_task_app_users ptau on pt.id = ptau.project_task_id
                         LEFT JOIN users u on ptau.user_id = u.id

                WHERE w.slug = @workspaceKey

                ORDER BY p.id, b.identifier, bg.sort_order, ptibg.sort_order;
            ", new
        {
            workspaceKey,
        });

        return RowsToExportList(rows);
    }

    public async Task<List<ExportTaskViewModel>> GetBoardExportTasksAsync(string workspaceKey, string boardIdentifier)
    {
        using var connection = ConnectionFactory.StartConnection();

        var rows = await connection.QueryAsync<TasksViewRowMap>(@"
                SELECT w.slug              AS workspace_key
                     , p.name              AS project_name
                     , p.key               AS project_key
                     , b.name              AS board_name
                     , b.identifier        AS board_identifier
                     , pt.id               AS task_id
                     , pt.name             AS task_name
                     , pt.description      AS task_description
                     , pt.is_flagged       AS task_is_flagged
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

                FROM workspaces w
                         LEFT JOIN projects p on p.workspace_id = w.id
                         LEFT JOIN boards b on b.project_id = p.id AND b.identifier = @boardIdentifier
                         LEFT JOIN board_groups bg ON b.id = bg.board_id AND NOT bg.is_deleted
                         LEFT JOIN project_task_in_board_groups ptibg on bg.id = ptibg.board_group_id
                         LEFT JOIN project_tasks pt on pt.id = ptibg.project_task_id AND NOT pt.is_deleted
                         INNER JOIN users o on pt.owner_id = o.id
                         LEFT JOIN project_task_tags ptt on pt.id = ptt.project_task_id
                         LEFT JOIN tags t on ptt.tag_id = t.id AND NOT t.is_deleted
                         LEFT JOIN project_task_app_users ptau on pt.id = ptau.project_task_id
                         LEFT JOIN users u on ptau.user_id = u.id

                WHERE w.slug = @workspaceKey

                ORDER BY bg.sort_order, ptibg.sort_order, t.name, u.id;;
            ", new
        {
            workspaceKey,
            boardIdentifier,
        });

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

            if (lastTask?.SystemId is {} && systemId == lastTask.SystemId)
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
                IsFlagged = row.Task_Is_Flagged,
                SortOrder = row.Task_Sort_Order,
                Board = row.Board_Identifier,
                CreatedAt = row.Task_Created_At,
                UpdatedAt = row.Task_Updated_At,
                Assignees = Set(row.Assignee_Email),
                Owner = row.Owner_Email,
                Project = row.Project_Name,
                Group = row.Board_Group_Name,
                Tags = Set(row.Tag),
            });

            return result;
        });
    }

    public async Task<List<int>> GetTaskIdsInBoard(string boardIdentifier)
    {
        using var connection = ConnectionFactory.StartConnection();

        var results = await connection.QueryAsync<int>(@"
                SELECT pt.id
                FROM boards b

                INNER JOIN board_groups bg ON b.id = bg.board_id AND NOT bg.is_deleted
                INNER JOIN project_task_in_board_groups ptibg on bg.id = ptibg.board_group_id
                INNER JOIN project_tasks pt on pt.id = ptibg.project_task_id AND NOT pt.is_deleted

                WHERE b.identifier = @boardIdentifier
            ", new { boardIdentifier });

        return results.ToList();
    }

    public async Task<int?> GetNextScopeId(int projectId, int increment = 0)
    {
        var taskCount = await Entities
            .Where(x => x.ProjectId == projectId)
            .OrderByDescending(x => x.ProjectScopeId)
            .Select(x => x.ProjectScopeId)
            .FirstOrDefaultAsync();

        return taskCount + 1 + increment;
    }

    public async Task<ActivityAncestors> GetAncestors(int taskId)
    {
        using var connection = ConnectionFactory.StartConnection();

        var result = await connection.QueryFirstAsync<TaskAncestorRow>(@"
                SELECT
                      ptibg.project_task_id as task_id
                    , ptibg.board_group_id as board_group_id
                    , b.id as board_id
                    , p.id as project_id
                    , p.workspace_id as workspace_id
                FROM project_task_in_board_groups ptibg
                LEFT JOIN board_groups bg on bg.id = ptibg.board_group_id
                LEFT JOIN boards b on b.id = bg.board_id
                LEFT Join projects p on p.id = b.project_id
                WHERE ptibg.project_task_id = @taskId
            ", new { taskId });

        return new ActivityAncestors
        {
            TaskId = result.Task_id,
            BoardGroupId = result.Board_group_id,
            BoardId = result.Board_id,
            ProjectId = result.Project_id,
            WorkspaceId = result.Workspace_id,
        };
    }
}
