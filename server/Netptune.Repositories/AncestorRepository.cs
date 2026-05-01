using Dapper;

using Netptune.Core.Models.Activity;
using Netptune.Core.Repositories;
using Netptune.Core.Repositories.Common;
using Netptune.Repositories.Common;
using Netptune.Repositories.RowMaps;

namespace Netptune.Repositories;

public class AncestorRepository : ReadOnlyRepository, IAncestorRepository
{
    public AncestorRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory)
    {
    }

    public async Task<ActivityAncestors> GetProjectTaskAncestors(int taskId, CancellationToken cancellationToken = default)
    {
        using var connection = ConnectionFactory.StartConnection();

        var result = await connection.QueryFirstOrDefaultAsync<TaskAncestorRow>(new CommandDefinition(@"
                SELECT
                      ptibg.project_task_id as task_id
                    , ptibg.board_group_id as board_group_id
                    , b.id as board_id
                    , b.identifier as board_key
                    , p.id as project_id
                    , p.key as project_key
                    , p.workspace_id as workspace_id
                    , w.slug as workspace_key
                    , pt.project_scope_id as task_scope_id
                FROM project_task_in_board_groups ptibg
                LEFT JOIN board_groups bg on bg.id = ptibg.board_group_id
                LEFT JOIN boards b on b.id = bg.board_id
                LEFT JOIN projects p on p.id = b.project_id
                LEFT JOIN workspaces w on w.id = p.workspace_id
                LEFT JOIN project_tasks pt on pt.id = ptibg.project_task_id
                WHERE ptibg.project_task_id = @taskId
            ", new { taskId }, cancellationToken: cancellationToken));

        if (result is null) return new ActivityAncestors();

        return new ActivityAncestors
        {
            TaskId = result.Task_id,
            BoardGroupId = result.Board_group_id,
            BoardId = result.Board_id,
            BoardKey = result.Board_key,
            ProjectId = result.Project_id,
            ProjectKey = result.Project_key,
            WorkspaceId = result.Workspace_id,
            WorkspaceKey = result.Workspace_key,
            TaskScopeId = result.Task_scope_id,
        };
    }

    public async Task<ActivityAncestors> GetBoardGroupAncestors(int boardGroupId, CancellationToken cancellationToken = default)
    {
        using var connection = ConnectionFactory.StartConnection();

        var result = await connection.QueryFirstOrDefaultAsync<BoardGroupAncestorRow>(new CommandDefinition(@"
                SELECT
                      bg.id as board_group_id
                    , b.id as board_id
                    , b.identifier as board_key
                    , p.id as project_id
                    , p.key as project_key
                    , p.workspace_id as workspace_id
                    , w.slug as workspace_key
                FROM board_groups bg
                LEFT JOIN boards b on b.id = bg.board_id
                LEFT JOIN projects p on p.id = b.project_id
                LEFT JOIN workspaces w on w.id = p.workspace_id
                WHERE bg.id = @boardGroupId
            ", new { boardGroupId }, cancellationToken: cancellationToken));

        if (result is null) return new ActivityAncestors();

        return new ActivityAncestors
        {
            BoardGroupId = result.Board_group_id,
            BoardId = result.Board_id,
            BoardKey = result.Board_key,
            ProjectId = result.Project_id,
            ProjectKey = result.Project_key,
            WorkspaceId = result.Workspace_id,
            WorkspaceKey = result.Workspace_key,
        };
    }

    public async Task<ActivityAncestors> GetBoardAncestors(int boardId, CancellationToken cancellationToken = default)
    {
        using var connection = ConnectionFactory.StartConnection();

        var result = await connection.QueryFirstOrDefaultAsync<BoardAncestorRow>(new CommandDefinition(@"
                SELECT
                      b.id as board_id
                    , b.identifier as board_key
                    , p.id as project_id
                    , p.key as project_key
                    , p.workspace_id as workspace_id
                    , w.slug as workspace_key
                FROM boards b
                LEFT JOIN projects p on p.id = b.project_id
                LEFT JOIN workspaces w on w.id = p.workspace_id
                WHERE b.id = @boardId
            ", new { boardId }, cancellationToken: cancellationToken));

        if (result is null) return new ActivityAncestors();

        return new ActivityAncestors
        {
            BoardId = result.Board_id,
            BoardKey = result.Board_key,
            ProjectId = result.Project_id,
            ProjectKey = result.Project_key,
            WorkspaceId = result.Workspace_id,
            WorkspaceKey = result.Workspace_key,
        };
    }

    public async Task<ActivityAncestors> GetProjectAncestors(int projectId, CancellationToken cancellationToken = default)
    {
        using var connection = ConnectionFactory.StartConnection();

        var result = await connection.QueryFirstOrDefaultAsync<ProjectAncestorRow>(new CommandDefinition(@"
                SELECT
                      p.id as project_id
                    , p.key as project_key
                    , p.workspace_id as workspace_id
                    , w.slug as workspace_key
                FROM projects p
                LEFT JOIN workspaces w on w.id = p.workspace_id
                WHERE p.id = @projectId
            ", new { projectId }, cancellationToken: cancellationToken));

        if (result is null) return new ActivityAncestors();

        return new ActivityAncestors
        {
            ProjectId = result.Project_id,
            ProjectKey = result.Project_key,
            WorkspaceId = result.Workspace_id,
            WorkspaceKey = result.Workspace_key,
        };
    }
}
