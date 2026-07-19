using Mediator;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Events;
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

        var taskEntity = await UnitOfWork.Tasks.GetAsync(request.TaskId, cancellationToken: cancellationToken);

        if (taskEntity is null)
        {
            return ClientResponse<SprintDetailViewModel>.NotFound;
        }

        await UnitOfWork.Transaction(async () =>
        {
            taskEntity.SprintId = null;

            if (sprint.Status == SprintStatus.Active)
            {
                await EventRecords.Append(new EventWriteRequest<ScopeMemberChangedPayload>
                {
                    WorkspaceId = sprint.WorkspaceId,
                    EventKey = EventKeys.ScopeMemberChanged,
                    SubjectType = EventEntityTypes.From(EntityType.Sprint),
                    SubjectId = sprint.Id.ToString(),
                    Payload = new ScopeMemberChangedPayload
                    {
                        Change = "removed",
                        MemberType = EventEntityTypes.From(EntityType.Task),
                        MemberId = task.Id.ToString(),
                        EstimateType = task.EstimateType?.ToString(),
                        EstimateValue = task.EstimateValue,
                        StatusId = task.StatusId,
                        StatusCategory = task.StatusCategory.ToString(),
                    },
                    References =
                    [
                        new EventReferenceInput
                        {
                            Role = EventReferenceRoles.Member,
                            EntityType = EventEntityTypes.From(EntityType.Task),
                            EntityId = task.Id.ToString(),
                        },
                        new EventReferenceInput
                        {
                            Role = EventReferenceRoles.Scope,
                            EntityType = EventEntityTypes.From(EntityType.Project),
                            EntityId = sprint.ProjectId.ToString(),
                        },
                    ],
                }, cancellationToken);
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
