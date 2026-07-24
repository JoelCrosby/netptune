using System.Text.Json;

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
using Netptune.Repositories.Sql;

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

        // websearch_to_tsquery treats spaces as AND; join words with "or" to preserve
        // the original match-any-word behaviour while staying injection-safe.
        var searchPhrase = string.IsNullOrWhiteSpace(searchTerm)
            ? null
            : string.Join(" or ", searchTerm.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries));

        var results = await connection.QueryMultipleAsync(new CommandDefinition(
            SqlScripts.GetBoardView,
            new { boardId, searchPhrase, sprintId, taskEntityType = EntityType.Task },
            cancellationToken: cancellationToken));

        var rows = results.Read<BoardViewRowMap>();
        var meta = results.ReadFirstOrDefault<BoardViewMetaRowMap>();

        if (meta is null) return null;

        // Rows arrive ordered by board group then task, with tags/assignees already
        // aggregated per task in SQL, so a single linear pass rebuilds the tree.
        var groups = new List<BoardViewGroup>(200);
        BoardViewGroup? currentGroup = null;

        foreach (var row in rows)
        {
            if (currentGroup is null || currentGroup.Id != row.Board_Group_Id)
            {
                currentGroup = new BoardViewGroup
                {
                    Id = row.Board_Group_Id,
                    Name = row.Board_Group_Name,
                    SortOrder = row.Board_Group_Sort_Order,
                    StatusId = row.Board_Group_Status_Id,
                    Tasks = new List<BoardViewTask>(),
                };

                groups.Add(currentGroup);
            }

            if (!row.Task_Id.HasValue) continue;

            currentGroup.Tasks.Add(new BoardViewTask
            {
                Id = row.Task_Id.Value,
                Name = row.Task_Name,
                StatusId = row.Task_Status_Id,
                StatusName = row.Task_Status_Name,
                StatusKey = row.Task_Status_Key,
                StatusColor = row.Task_Status_Color,
                StatusCategory = row.Task_Status_Category,
                SystemId = $"{meta.Project_Key}-{row.Project_Scope_Id}",
                Tags = row.Tags.ToList(),
                HasComments = row.Has_Comments,
                FlagCount = row.Flag_Count,
                Priority = row.Task_Priority,
                EstimateType = row.Task_Estimate_Type,
                EstimateValue = row.Task_Estimate_Value,
                StartDate = row.Task_Start_Date,
                DueDate = row.Task_Due_Date,
                CreatedAt = row.Task_Created_At,
                UpdatedAt = row.Task_Updated_At,
                SprintId = row.Sprint_Id,
                SprintName = row.Sprint_Name,
                SprintStatus = row.Sprint_Status,
                SortOrder = row.Task_Sort_Order,
                ProjectId = row.Project_Id,
                WorkspaceId = row.Workspace_Id,
                WorkspaceKey = meta.Workspace_Identifier,
                Assignees = ParseAssignees(row.Assignees),
            });
        }

        return groups;
    }

    private static readonly JsonSerializerOptions AssigneeJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private static List<AssigneeViewModel> ParseAssignees(string assigneesJson)
    {
        var assignees = JsonSerializer.Deserialize<List<BoardViewAssigneeRowMap>>(assigneesJson, AssigneeJsonOptions);

        if (assignees is null) return new List<AssigneeViewModel>();

        return assignees.ConvertAll(assignee => new AssigneeViewModel
        {
            Id = assignee.Id,
            DisplayName = $"{assignee.Firstname} {assignee.Lastname}",
            PictureUrl = assignee.Picture_Url,
            IsServiceAccount = assignee.Is_Service_Account,
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
            .Select(group => new BoardGroupTaskTarget
            {
                Id = group.Id,
                Name = group.Name,
                MaxSortOrder = Context.ProjectTaskInBoardGroups
                    .Where(taskInGroup => taskInGroup.BoardGroupId == group.Id)
                    .Max(taskInGroup => (double?)taskInGroup.SortOrder) ?? 0D,
                WorkspaceId = group.WorkspaceId,
                StatusId = group.StatusId,
                ProjectId = group.Board != null ? group.Board.ProjectId : null,
            })
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
            .Select(group => new BoardGroupTaskTarget
            {
                Id = group.Id,
                Name = group.Name,
                MaxSortOrder = Context.ProjectTaskInBoardGroups
                    .Where(taskInGroup => taskInGroup.BoardGroupId == group.Id)
                    .Max(taskInGroup => (double?)taskInGroup.SortOrder) ?? 0D,
                WorkspaceId = group.WorkspaceId,
                StatusId = group.StatusId,
                ProjectId = group.Board != null ? group.Board.ProjectId : null,
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<List<BoardGroupOptionViewModel>> GetOptionsInWorkspace(
        int workspaceId,
        CancellationToken cancellationToken = default)
    {
        return Entities
            .Where(group => group.WorkspaceId == workspaceId && !group.IsDeleted)
            .Where(group => group.Board != null && !group.Board.IsDeleted && group.Board.Project != null)
            .OrderBy(group => group.Board!.Project!.Name)
            .ThenBy(group => group.Board!.Name)
            .ThenBy(group => group.SortOrder)
            .Select(group => new BoardGroupOptionViewModel
            {
                Id = group.Id,
                Name = group.Name,
                BoardName = group.Board!.Name,
                ProjectName = group.Board.Project!.Name,
            })
            .ToListAsync(cancellationToken);
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
