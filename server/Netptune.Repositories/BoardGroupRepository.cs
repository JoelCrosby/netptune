using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Dapper;

using Microsoft.EntityFrameworkCore;

using Netptune.Core.Entities;
using Netptune.Core.Repositories;
using Netptune.Core.Repositories.Common;
using Netptune.Core.ViewModels.Boards;
using Netptune.Entities.Contexts;
using Netptune.Repositories.Common;
using Netptune.Repositories.RowMaps;

namespace Netptune.Repositories
{
    public class BoardGroupRepository : Repository<DataContext, BoardGroup, int>, IBoardGroupRepository
    {
        public BoardGroupRepository(DataContext dataContext, IDbConnectionFactory connectionFactories)
            : base(dataContext, connectionFactories)
        {
        }

        public Task<List<BoardGroup>> GetBoardGroupsInBoard(int boardId, bool isReadonly = false)
        {
            var query = Entities

                .Where(boardGroup => boardGroup.BoardId == boardId)
                .Where(boardGroup => !boardGroup.IsDeleted)

                .OrderBy(boardGroup => boardGroup.SortOrder)

                .Include(group => group.TasksInGroups)
                .ThenInclude(relational => relational.ProjectTask)
                .ThenInclude(task => task.Owner)

                .Include(group => group.TasksInGroups)
                .ThenInclude(relational => relational.ProjectTask)
                .ThenInclude(task => task.Assignee)

                .Include(group => group.TasksInGroups)
                .ThenInclude(relational => relational.ProjectTask)
                .ThenInclude(task => task.Project)

                .Include(group => group.TasksInGroups)
                .ThenInclude(relational => relational.ProjectTask)
                .ThenInclude(task => task.Workspace)

                .Include(group => group.TasksInGroups)
                .ThenInclude(relational => relational.ProjectTask)
                .ThenInclude(task => task.ProjectTaskTags)
                .ThenInclude(taskTag => taskTag.Tag);

            return query.ApplyReadonly(isReadonly);
        }

        public async Task<List<BoardViewGroup>> GetBoardView(int boardId)
        {
            using var connection = ConnectionFactory.StartConnection();

            var results = await connection.QueryMultipleAsync(@"
                SELECT b.id
                     , b.name
                     , b.identifier
                     , pt.id               AS task_id
                     , pt.name             AS task_name
                     , pt.is_flagged
                     , pt.project_scope_id AS project_scope_id
                     , pt.status           AS task_status
                     , ptibg.sort_order    AS sort_order
                     , bg.id               AS board_group_id
                     , bg.name             AS board_group_name
                     , bg.type             AS board_group_type
                     , u.id                AS assignee_id
                     , u.firstname
                     , u.lastname
                     , u.picture_url
                     , t.name              AS tag
                     , pt.workspace_id     AS workspace_id
                     , pt.project_id       AS project_id

                FROM boards b

                         LEFT JOIN board_groups bg ON b.id = bg.board_id AND NOT bg.is_deleted
                         LEFT JOIN project_task_in_board_groups ptibg on bg.id = ptibg.board_group_id
                         LEFT JOIN project_tasks pt on pt.id = ptibg.project_task_id AND NOT pt.is_deleted
                         LEFT JOIN users u on pt.assignee_id = u.id
                         LEFT JOIN project_task_tags ptt on pt.id = ptt.project_task_id
                         LEFT JOIN tags t on ptt.tag_id = t.id AND NOT t.is_deleted

                WHERE b.id = @boardId

                ORDER BY bg.sort_order, ptibg.sort_order;

                SELECT w.slug AS workspace_identifier
                     , p.key  AS project_key
                FROM boards b

                         LEFT JOIN workspaces w on b.workspace_id = w.id
                         LEFT JOIN projects p on b.project_id = p.id
                WHERE b.id = @boardId
            ", new { boardId });

            var rows = results.Read<BoardViewRowMap>();
            var meta = results.ReadFirstOrDefault<BoardViewMetaRowMap>();

            if (meta is null) return null;

            return rows.Aggregate(new List<BoardViewGroup>(200), (result, row) =>
            {
                var lastGroup = result.LastOrDefault();
                var lastTask = lastGroup?.Tasks.LastOrDefault();

                if (lastTask?.Id is { } && row.Task_Id == lastTask.Id)
                {
                    lastTask.Tags.Add(row.Tag);
                    return result;
                }

                if (row.Board_Group_Id == lastGroup?.Id)
                {
                    lastGroup.Tasks.Add(new BoardViewTask
                    {
                        Id = row.Task_Id,
                        Name = row.Task_Name,
                        Status = row.Task_Status,
                        SystemId = $"{meta.Project_Key}-{row.Project_Scope_Id}",
                        AssigneeUsername = $"{row.Firstname} {row.Lastname}",
                        AssigneePictureUrl = row.Picture_Url,
                        AssigneeId = row.Assignee_Id,
                        Tags = row.Tag is { } ? new List<string> { row.Tag } : new List<string>(),
                        IsFlagged = row.Is_Flagged,
                        SortOrder = row.Sort_Order,
                        ProjectId = row.Project_Id,
                        WorkspaceId = row.Workspace_Id,
                        WorkspaceSlug = meta.Workspace_Identifier
                    });

                    return result;
                }

                result.Add(new BoardViewGroup
                {
                    Id = row.Board_Group_Id,
                    Name = row.Board_Group_Name,
                    SortOrder = result.Count,
                    Type = row.Board_Group_Type,
                    Tasks = row.Task_Name is null ? new List<BoardViewTask>() : new List<BoardViewTask>(100)
                    {
                        new BoardViewTask
                        {
                            Id = row.Task_Id,
                            Name = row.Task_Name,
                            Status = row.Task_Status,
                            SystemId = $"{meta.Project_Key}-{row.Project_Scope_Id}",
                            AssigneeUsername = $"{row.Firstname} {row.Lastname}",
                            AssigneePictureUrl = row.Picture_Url,
                            AssigneeId = row.Assignee_Id,
                            Tags = row.Tag is { } ? new List<string> { row.Tag } : new List<string>(),
                            IsFlagged = row.Is_Flagged,
                            SortOrder = row.Sort_Order,
                            ProjectId = row.Project_Id,
                            WorkspaceId = row.Workspace_Id,
                            WorkspaceSlug = meta.Workspace_Identifier
                        }
                    }
                });

                return result;
            });
        }

        public Task<List<BoardGroup>> GetBoardGroupsForProjectTask(int taskId, bool isReadonly = false)
        {
            var query = Entities

                .Where(group => group.TasksInGroups
                    .Select(x => x.ProjectTaskId)
                    .Contains(taskId))

                .Include(group => group.TasksInGroups)
                    .ThenInclude(relational => relational.ProjectTask);

            return query.ApplyReadonly(isReadonly);
        }

        public Task<List<ProjectTask>> GetTasksInGroup(int groupId, bool isReadonly = false)
        {
            var query = Context.ProjectTaskInBoardGroups
                .Where(item => item.BoardGroupId == groupId)
                .Select(item => item.ProjectTask);

            return query.ApplyReadonly(isReadonly);
        }

        public async ValueTask<double> GetBoardGroupDefaultSortOrder(int boardId)
        {
            var sortOrders = await Entities

                .Where(boardGroup => boardGroup.BoardId == boardId)
                .Where(boardGroup => !boardGroup.IsDeleted)

                .OrderBy(boardGroup => boardGroup.SortOrder)

                .AsNoTracking()

                .Select(boardGroup => boardGroup.SortOrder)
                .ToListAsync();

            return sortOrders.Max() + 1;
        }
    }
}