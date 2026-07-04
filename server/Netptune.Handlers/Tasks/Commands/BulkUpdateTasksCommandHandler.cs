using Mediator;

using Microsoft.Extensions.Logging;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Relationships;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.Tasks.Commands;

public sealed record BulkUpdateTasksCommand(BulkUpdateTasksRequest Request) : IRequest<ClientResponse>;

public sealed class BulkUpdateTasksCommandHandler : IRequestHandler<BulkUpdateTasksCommand, ClientResponse>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly ILogger<BulkUpdateTasksCommandHandler> Logger;

    public BulkUpdateTasksCommandHandler(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        ILogger<BulkUpdateTasksCommandHandler> logger)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Logger = logger;
    }

    public async ValueTask<ClientResponse> Handle(BulkUpdateTasksCommand command, CancellationToken cancellationToken)
    {
        var req = command.Request;
        var workspaceId = await Identity.GetWorkspaceId();

        // Scope to tasks that actually belong to the caller's workspace.
        var taskIds = await UnitOfWork.Tasks.GetValidTaskIdsInWorkspace(req.TaskIds, workspaceId, cancellationToken);

        if (taskIds.Count == 0) return ClientResponse.Success;

        var tasks = await UnitOfWork.Tasks.GetTasksForUpdate(taskIds, cancellationToken);

        // Resolve the target status once — it applies to every selected task.
        var status = req.StatusId.HasValue
            ? await UnitOfWork.Statuses.GetInWorkspace(req.StatusId.Value, workspaceId, cancellationToken: cancellationToken)
            : null;

        // Tasks moved to the same project each need a distinct scope id; track per-project increments.
        var scopeIncrements = new Dictionary<int, int>();

        await UnitOfWork.Transaction(async () =>
        {
            foreach (var task in tasks)
            {
                var projectChanged = req.ProjectId.HasValue && task.ProjectId != req.ProjectId.Value;

                if (status is not null)
                {
                    task.StatusId = status.Id;
                }

                if (req.Priority.HasValue) task.Priority = req.Priority;
                if (req.EstimateType.HasValue) task.EstimateType = req.EstimateType;
                if (req.EstimateValue.HasValue) task.EstimateValue = req.EstimateValue;

                if (req.ClearSprint)
                {
                    task.SprintId = null;
                }
                else if (req.SprintId.HasValue)
                {
                    task.SprintId = req.SprintId;
                }

                if (projectChanged)
                {
                    await MoveToProject(task, req.ProjectId!.Value, scopeIncrements, cancellationToken);
                }

                if (req.AssigneeIds is not null)
                {
                    task.ProjectTaskAppUsers = ProjectTaskAppUser.MergeUsersIds(
                        task.Id,
                        task.ProjectTaskAppUsers,
                        req.AssigneeIds).ToList();
                }

                // Moving a task to a different project invalidates its board-group
                // membership, which belongs to the old project's board.
                if (projectChanged)
                {
                    await RepositionInBoardGroup(task, cancellationToken);
                }
            }

            await UnitOfWork.CompleteAsync(cancellationToken);
        });

        return ClientResponse.Success;
    }

    private async Task MoveToProject(ProjectTask task, int projectId, Dictionary<int, int> scopeIncrements, CancellationToken cancellationToken)
    {
        var increment = scopeIncrements.GetValueOrDefault(projectId);
        var nextScopeId = await UnitOfWork.Tasks.GetNextScopeId(projectId, increment, cancellationToken);

        if (nextScopeId.HasValue)
        {
            task.ProjectScopeId = nextScopeId.Value;
        }

        scopeIncrements[projectId] = increment + 1;
        task.ProjectId = projectId;
    }

    private async Task RepositionInBoardGroup(ProjectTask task, CancellationToken cancellationToken)
    {
        if (task.ProjectId is null) return;

        var group = await UnitOfWork.BoardGroups.GetDefaultTaskTarget(task.ProjectId.Value, cancellationToken);

        if (group is null)
        {
            Logger.LogInformation(
                "Project with id {ProjectId} does not have a default board group",
                task.ProjectId.Value);
            return;
        }

        await UnitOfWork.ProjectTasksInGroups.DeleteAllByTaskId([task.Id], cancellationToken);

        await UnitOfWork.ProjectTasksInGroups.AddAsync(new ProjectTaskInBoardGroup
        {
            BoardGroupId = group.Id,
            ProjectTaskId = task.Id,
            SortOrder = group.MaxSortOrder + 1,
        }, cancellationToken);
    }
}
