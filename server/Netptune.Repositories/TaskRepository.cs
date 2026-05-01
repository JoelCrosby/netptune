using System.Linq.Expressions;

using Dapper;

using Microsoft.EntityFrameworkCore;

using Netptune.Core.Entities;
using Netptune.Core.Repositories;
using Netptune.Core.Repositories.Common;
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
            .Include(x => x.Owner)
            .Include(x => x.Workspace)
            .AsSplitQuery()
            .IsReadonly(isReadonly)
            .FirstOrDefaultAsync(EqualsPredicate(id), cancellationToken);
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

    public Task<List<TaskViewModel>> GetTasksAsync(string workspaceKey, bool isReadonly = false, CancellationToken cancellationToken = default)
    {
        return Entities
            .Where(x => x.Workspace.Slug == workspaceKey && !x.IsDeleted)
            .OrderByDescending(x => x.UpdatedAt)
            .Select(TaskToViewModel())
            .ToReadonlyListAsync(isReadonly, cancellationToken);
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

}
