using Mediator;

using Netptune.Core.Enums;
using Netptune.Core.Events.Sprints;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Sprints;

namespace Netptune.Handlers.Sprints.Commands;

public sealed record RemoveTaskFromSprintCommand(int SprintId, int TaskId) : IRequest<ClientResponse<SprintDetailViewModel>>;

public sealed class RemoveTaskFromSprintCommandHandler : IRequestHandler<RemoveTaskFromSprintCommand, ClientResponse<SprintDetailViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IActivityLogger Activity;
    private readonly IEventRecordWriter EventRecords;

    public RemoveTaskFromSprintCommandHandler(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        IActivityLogger activity,
        IEventRecordWriter eventRecords)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Activity = activity;
        EventRecords = eventRecords;
    }

    public async ValueTask<ClientResponse<SprintDetailViewModel>> Handle(RemoveTaskFromSprintCommand request, CancellationToken cancellationToken)
    {
        var workspaceKey = Identity.GetWorkspaceKey();
        var sprint = await UnitOfWork.Sprints.GetSprintInWorkspaceAsync(workspaceKey, request.SprintId, cancellationToken: cancellationToken);

        if (sprint is null)
        {
            return ClientResponse<SprintDetailViewModel>.NotFound;
        }

        if (sprint.Status == SprintStatus.Completed)
        {
            return ClientResponse<SprintDetailViewModel>.Failed("Completed sprints cannot be changed");
        }

        var task = await UnitOfWork.Tasks.GetTaskViewModel(request.TaskId, cancellationToken);

        if (task is null || task.SprintId != sprint.Id)
        {
            return ClientResponse<SprintDetailViewModel>.NotFound;
        }

        var scope = new SprintScope(sprint.WorkspaceId, sprint.Id, sprint.ProjectId);

        await UnitOfWork.Transaction(async () =>
        {
            // The sprint above is tracked with its tasks included, so clearing the foreign key on a
            // tracked task is fixed straight back up from that collection and the removal is
            // silently lost. Write it the same way assignment does instead.
            await UnitOfWork.Tasks.RemoveTasksFromSprint([request.TaskId], cancellationToken);

            if (sprint.Status == SprintStatus.Active)
            {
                var member = new SprintMember
                {
                    TaskId = task.Id,
                    StatusId = task.StatusId,
                    StatusCategory = task.StatusCategory.ToString(),
                    EstimateType = task.EstimateType?.ToString(),
                    EstimateValue = task.EstimateValue,
                };

                var removed = SprintMemberEvents.Changed(scope, member, SprintMemberChanges.Removed);

                await EventRecords.Append(removed, cancellationToken);
            }

            await UnitOfWork.CompleteAsync(cancellationToken);
        });

        var result = await UnitOfWork.Sprints.GetSprintDetailAsync(workspaceKey, sprint.Id, cancellationToken);

        Activity.Log(options =>
        {
            options.EntityId = sprint.Id;
            options.EntityType = EntityType.Sprint;
            options.Type = ActivityType.Unassign;
        });

        return result is null
            ? ClientResponse<SprintDetailViewModel>.NotFound
            : ClientResponse<SprintDetailViewModel>.Success(result);
    }
}
