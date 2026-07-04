using Dapper;

using Microsoft.EntityFrameworkCore;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Models.ProjectTasks;
using Netptune.Core.Repositories;
using Netptune.Core.Repositories.Common;
using Netptune.Core.ViewModels.Boards;
using Netptune.Core.ViewModels.Users;
using Netptune.Entities.Contexts;
using Netptune.Repositories.Common;
using Netptune.Repositories.RowMaps;

namespace Netptune.Repositories;

public class BoardGroupRepository : WorkspaceEntityRepository<DataContext, BoardGroup, int>, IBoardGroupRepository
{
    public BoardGroupRepository(DataContext dataContext, IDbConnectionFactory connectionFactories)
        : base(dataContext, connectionFactories)
    {
    }

    public Task<BoardGroup?> GetWithTasksInGroups(int id, CancellationToken cancellationToken = default)
    {
        return Entities
            .Include(group => group.TasksInGroups)
            .FirstOrDefaultAsync(group => group.Id == id, cancellationToken);
    }

    public Task<List<BoardGroup>> GetBoardGroupsInBoard(int boardId, bool isReadonly = false, CancellationToken cancellationToken = default)
    {
        return Entities
            .Where(boardGroup => boardGroup.BoardId == boardId)
            .Where(boardGroup => !boardGroup.IsDeleted)
            .Include(boardGroup => boardGroup.TasksInGroups)
            .OrderBy(boardGroup => boardGroup.SortOrder)
            .ToReadonlyListAsync(isReadonly, cancellationToken);
    }

    public async Task<List<BoardViewGroup>?> GetBoardViewGroups(
        int boardId,
        string? searchTerm = null,
        int? sprintId = null,
        CancellationToken cancellationToken = default)
    {
        using var connection = ConnectionFactory.StartConnection();

        var searchPhrase = searchTerm?.Trim().Replace(" ", " | ");

        var searchQuery = searchTerm is null
            ? null
            : "AND to_tsvector('english', pt.name) @@ to_tsquery('english', @searchPhrase)";

        var results = await connection.QueryMultipleAsync(new CommandDefinition(@$"
                WITH board_groups_for_board AS (
                    SELECT bg.id
                         , bg.name
                         , bg.type
                         , bg.sort_order
                    FROM board_groups bg
                    WHERE bg.board_id = @boardId
                      AND NOT bg.is_deleted
                ),
                limited_tasks AS (
                    SELECT pt.id               AS task_id
                         , pt.name             AS task_name
                         , pt.priority         AS task_priority
                         , pt.estimate_type    AS task_estimate_type
                         , pt.estimate_value   AS task_estimate_value
                         , s.id                AS sprint_id
                         , s.name              AS sprint_name
                         , s.status            AS sprint_status
                         , pt.project_scope_id AS project_scope_id
                         , pt.status_id        AS task_status_id
                         , st.name             AS task_status_name
                         , st.key              AS task_status_key
                         , st.color            AS task_status_color
                         , st.category         AS task_status_category
                         , ptibg.sort_order    AS task_sort_order
                         , bg.id               AS board_group_id
                         , pt.workspace_id     AS workspace_id
                         , pt.project_id       AS project_id
                    FROM board_groups_for_board bg
                             INNER JOIN project_task_in_board_groups ptibg on bg.id = ptibg.board_group_id
                             INNER JOIN project_tasks pt on pt.id = ptibg.project_task_id
                                AND NOT pt.is_deleted
                             INNER JOIN statuses st on pt.status_id = st.id
                             LEFT JOIN sprints s on pt.sprint_id = s.id AND NOT s.is_deleted
                    WHERE (@sprintId IS NULL OR pt.sprint_id = @sprintId)
                      {searchQuery}
                    ORDER BY bg.sort_order, ptibg.sort_order, pt.id
                )
                SELECT b.id
                     , b.name              AS board_name
                     , b.identifier        AS board_identifier
                     , lt.task_id
                     , lt.task_name
                     , lt.task_priority
                     , lt.task_estimate_type
                     , lt.task_estimate_value
                     , lt.sprint_id
                     , lt.sprint_name
                     , lt.sprint_status
                     , lt.project_scope_id
                     , lt.task_status_id
                     , lt.task_status_name
                     , lt.task_status_key
                     , lt.task_status_color
                     , lt.task_status_category
                     , lt.task_sort_order
                     , bg.id               AS board_group_id
                     , bg.name             AS board_group_name
                     , bg.sort_order       AS board_group_sort_order
                     , u.id                AS assignee_id
                     , u.firstname         AS assignee_firstname
                     , u.lastname          AS assignee_lastname
                     , u.picture_url       AS assignee_picture_url
                     , t.name              AS tag
                     , lt.workspace_id
                     , lt.project_id

                FROM boards b

                         LEFT JOIN board_groups_for_board bg ON TRUE
                         LEFT JOIN limited_tasks lt on bg.id = lt.board_group_id
                         LEFT JOIN project_task_tags ptt on lt.task_id = ptt.project_task_id
                         LEFT JOIN tags t on ptt.tag_id = t.id AND NOT t.is_deleted
                         LEFT JOIN project_task_app_users ptau on lt.task_id = ptau.project_task_id
                         LEFT JOIN users u on ptau.user_id = u.id

                WHERE b.id = @boardId

                ORDER BY bg.sort_order, lt.task_sort_order, t.name, u.id;

                -- Select board

                SELECT w.slug AS workspace_identifier
                     , p.key  AS project_key
                FROM boards b

                         LEFT JOIN workspaces w on b.workspace_id = w.id
                         LEFT JOIN projects p on b.project_id = p.id
                WHERE b.id = @boardId
            ", new { boardId, searchPhrase, sprintId }, cancellationToken: cancellationToken));

        var rows = results.Read<BoardViewRowMap>();
        var meta = results.ReadFirstOrDefault<BoardViewMetaRowMap>();

        if (meta is null) return null;

        return rows.Aggregate(new List<BoardViewGroup>(200), (result, row) =>
        {
            var lastGroup = result.LastOrDefault();
            var lastTask = lastGroup?.Tasks.LastOrDefault();
            var lastTag = lastTask?.Tags.LastOrDefault();
            var lastAssignee = lastTask?.Assignees.LastOrDefault();

            if (lastTask?.Id is not null && row.Task_Id.HasValue && row.Task_Id.Value == lastTask.Id)
            {
                if (lastTag != row.Tag && row.Tag is not null)
                {
                    lastTask.Tags.Add(row.Tag);
                }
                else if (lastAssignee?.Id != row.Assignee_Id)
                {
                    lastTask.Assignees.Add(new()
                    {
                        Id = row.Assignee_Id,
                        DisplayName = $"{row.Assignee_Firstname} {row.Assignee_Lastname}",
                        PictureUrl = row.Assignee_Picture_Url,
                    });
                }

                return result;
            }

            if (row.Board_Group_Id == lastGroup?.Id && row.Task_Id.HasValue)
            {
                lastGroup.Tasks.Add(new BoardViewTask
                {
                    Id = row.Task_Id.Value,
                    Name = row.Task_Name,
                    StatusId = row.Task_Status_Id,
                    StatusName = row.Task_Status_Name,
                    StatusKey = row.Task_Status_Key,
                    StatusColor = row.Task_Status_Color,
                    StatusCategory = row.Task_Status_Category,
                    SystemId = $"{meta.Project_Key}-{row.Project_Scope_Id}",
                    Tags = row.Tag is not null ? new List<string> { row.Tag } : new List<string>(),
                    Priority = row.Task_Priority,
                    EstimateType = row.Task_Estimate_Type,
                    EstimateValue = row.Task_Estimate_Value,
                    SprintId = row.Sprint_Id,
                    SprintName = row.Sprint_Name,
                    SprintStatus = row.Sprint_Status,
                    SortOrder = row.Task_Sort_Order,
                    ProjectId = row.Project_Id,
                    WorkspaceId = row.Workspace_Id,
                    WorkspaceKey = meta.Workspace_Identifier,
                    Assignees = new List<AssigneeViewModel>
                    {
                        new ()
                        {
                            Id = row.Assignee_Id,
                            DisplayName = $"{row.Assignee_Firstname} {row.Assignee_Lastname}",
                            PictureUrl = row.Assignee_Picture_Url,
                        },
                    },
                });

                return result;
            }

            if (row.Board_Group_Id == lastGroup?.Id && !row.Task_Id.HasValue)
            {
                return result;
            }

            result.Add(new BoardViewGroup
            {
                Id = row.Board_Group_Id,
                Name = row.Board_Group_Name,
                SortOrder = row.Board_Group_Sort_Order,
                Tasks = row.Task_Id is null ? new List<BoardViewTask>() : new List<BoardViewTask>(100)
                {
                    new()
                    {
                        Id = row.Task_Id.Value,
                        Name = row.Task_Name,
                        StatusId = row.Task_Status_Id,
                        StatusName = row.Task_Status_Name,
                        StatusKey = row.Task_Status_Key,
                        StatusColor = row.Task_Status_Color,
                        StatusCategory = row.Task_Status_Category,
                        SystemId = $"{meta.Project_Key}-{row.Project_Scope_Id}",
                        Tags = row.Tag is not null ? new List<string> { row.Tag } : new List<string>(),
                        SprintId = row.Sprint_Id,
                        SprintName = row.Sprint_Name,
                        SprintStatus = row.Sprint_Status,
                        SortOrder = row.Task_Sort_Order,
                        ProjectId = row.Project_Id,
                        WorkspaceId = row.Workspace_Id,
                        WorkspaceKey = meta.Workspace_Identifier,
                        Priority = row.Task_Priority,
                        Assignees = new List<AssigneeViewModel>
                        {
                            new ()
                            {
                                Id = row.Assignee_Id,
                                DisplayName = $"{row.Assignee_Firstname} {row.Assignee_Lastname}",
                                PictureUrl = row.Assignee_Picture_Url,
                            },
                        },
                    },
                },
            });

            return result;
        });
    }

    public Task<List<BoardGroup>> GetBoardGroupsForProjectTask(int taskId, bool isReadonly = false, CancellationToken cancellationToken = default)
    {
        var query = Entities

            .Where(group => group.TasksInGroups
                .Select(x => x.ProjectTaskId)
                .Contains(taskId))

            .Include(group => group.TasksInGroups)
            .ThenInclude(relational => relational.ProjectTask);

        return query.ToReadonlyListAsync(isReadonly, cancellationToken);
    }

    public Task<List<ProjectTask>> GetTasksInGroup(int groupId, bool isReadonly = false, CancellationToken cancellationToken = default)
    {
        var query = Context.ProjectTaskInBoardGroups
            .Where(item => item.BoardGroupId == groupId)
            .Select(item => item.ProjectTask!);

        return query.ToReadonlyListAsync(isReadonly, cancellationToken);
    }

    public Task<BoardGroupTaskTarget?> GetTaskTarget(int groupId, CancellationToken cancellationToken = default)
    {
        return Entities
            .Where(group => group.Id == groupId && !group.IsDeleted)
            .AsNoTracking()
            .Select(group => new BoardGroupTaskTarget(
                group.Id,
                group.Name,
                Context.ProjectTaskInBoardGroups
                    .Where(taskInGroup => taskInGroup.BoardGroupId == group.Id)
                    .Max(taskInGroup => (double?)taskInGroup.SortOrder) ?? 0D,
                group.WorkspaceId))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<BoardGroupTaskTarget?> GetDefaultTaskTarget(int projectId, CancellationToken cancellationToken = default)
    {
        return Entities
            .Where(group => !group.IsDeleted)
            .Where(group => group.Board != null
                && group.Board.ProjectId == projectId
                && group.Board.BoardType == BoardType.Default
                && !group.Board.IsDeleted)
            .OrderBy(group => group.SortOrder)
            .AsNoTracking()
            .Select(group => new BoardGroupTaskTarget(
                group.Id,
                group.Name,
                Context.ProjectTaskInBoardGroups
                    .Where(taskInGroup => taskInGroup.BoardGroupId == group.Id)
                    .Max(taskInGroup => (double?)taskInGroup.SortOrder) ?? 0D,
                group.WorkspaceId))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<double> GetMaxTaskSortOrder(int groupId, CancellationToken cancellationToken = default)
    {
        return await Context.ProjectTaskInBoardGroups
            .Where(taskInGroup => taskInGroup.BoardGroupId == groupId)
            .MaxAsync(taskInGroup => (double?)taskInGroup.SortOrder, cancellationToken) ?? 0D;
    }

    public async ValueTask<double> GetBoardGroupDefaultSortOrder(int boardId, CancellationToken cancellationToken = default)
    {
        var sortOrders = await Entities

            .Where(boardGroup => boardGroup.BoardId == boardId)
            .Where(boardGroup => !boardGroup.IsDeleted)

            .OrderBy(boardGroup => boardGroup.SortOrder)

            .AsNoTracking()

            .Select(boardGroup => boardGroup.SortOrder)
            .ToListAsync(cancellationToken);

        return sortOrders.Max() + 1;
    }

    public async Task<int?> GetBoardGroupIdForTask(int projectTaskId, CancellationToken cancellationToken = default)
    {
        var results = await Context.ProjectTaskInBoardGroups
            .AsNoTracking()
            .Where(x => x.ProjectTaskId == projectTaskId)
            .Select(x => x.BoardGroupId)
            .ToListAsync(cancellationToken);

        return results.Count > 0 ? results.Single() : null;
    }

    public async Task<int?> GetTaskAncestors(int projectTaskId)
    {
        var results = await Context.ProjectTaskInBoardGroups
            .AsNoTracking()
            .Where(x => x.ProjectTaskId == projectTaskId)
            .Select(x => x.BoardGroupId)
            .ToListAsync();

        return results.Count > 0 ? results.Single() : null;
    }
}
