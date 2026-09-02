using Mediator;

using Netptune.Core.Enums;
using Netptune.Core.Events.Tasks;
using Netptune.Core.Models.Search;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.Tasks.Commands;

public sealed record ReassignTasksCommand(ReassignTasksRequest Request) : IRequest<ClientResponse>;

public sealed class ReassignTasksCommandHandler : IRequestHandler<ReassignTasksCommand, ClientResponse>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IActivityLogger Activity;
    private readonly IIdentityService Identity;
    private readonly IEventPublisher EventPublisher;

    public ReassignTasksCommandHandler(
        INetptuneUnitOfWork unitOfWork,
        IActivityLogger activity,
        IIdentityService identity,
        IEventPublisher eventPublisher)
    {
        UnitOfWork = unitOfWork;
        Activity = activity;
        Identity = identity;
        EventPublisher = eventPublisher;
    }

    public async ValueTask<ClientResponse> Handle(ReassignTasksCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;
        var assigneeIds = req.AssigneeIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (assigneeIds.Count > 0)
        {
            var workspaceId = await Identity.GetWorkspaceId();
            var assignees = await UnitOfWork.Users.IsUserInWorkspaceRange(assigneeIds, workspaceId, cancellationToken);
            var validAssigneeIds = assignees.Select(assignee => assignee.Id).ToHashSet(StringComparer.Ordinal);
            var missingAssigneeIds = assigneeIds.Where(id => !validAssigneeIds.Contains(id)).ToList();

            if (missingAssigneeIds.Count > 0)
            {
                return ClientResponse.Failed($"Assignees were not found in the workspace: {string.Join(", ", missingAssigneeIds)}");
            }
        }

        var taskIdsInBoard = await UnitOfWork.Tasks.GetTaskIdsInBoard(req.BoardId, cancellationToken);
        var taskIds = req.TaskIds.Where(taskIdsInBoard.Contains).ToList();
        var replacedUserIds = await UnitOfWork.Tasks.ReplaceTaskAssignees(taskIds, assigneeIds, cancellationToken);

        await UnitOfWork.CompleteAsync(cancellationToken);

        foreach (var replacedUserId in replacedUserIds)
        {
            Activity.LogWithMany<AssignActivityMeta>(options =>
            {
                options.EntityIds = taskIds;
                options.EntityType = EntityType.Task;
                options.Type = ActivityType.Unassign;
                options.Meta = new AssignActivityMeta { AssigneeId = replacedUserId };
            });
        }

        foreach (var assigneeId in assigneeIds)
        {
            Activity.LogWithMany<AssignActivityMeta>(options =>
            {
                options.EntityIds = taskIds;
                options.EntityType = EntityType.Task;
                options.Type = ActivityType.Assign;
                options.Meta = new AssignActivityMeta { AssigneeId = assigneeId };
                options.RecipientUserIds = [assigneeId];
            });
        }

        var workspaceKey = Identity.GetWorkspaceKey();

        await EventPublisher.IndexTasks(taskIds, workspaceKey);

        return ClientResponse.Success;
    }
}
