using Mediator;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Events.Tasks;
using Netptune.Core.Events;
using Netptune.Core.Models.ProjectTasks;
using Netptune.Core.Relationships;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.ProjectTasks;

namespace Netptune.Handlers.Tasks.Commands;

public sealed record UpdateTaskCommand(UpdateProjectTaskRequest Request) : IRequest<ClientResponse<TaskViewModel>>;

public sealed class UpdateTaskCommandHandler : IRequestHandler<UpdateTaskCommand, ClientResponse<TaskViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IActivityLogger Activity;
    private readonly IEventPublisher EventPublisher;
    private readonly IIdentityService Identity;
    private readonly IEventRecordWriter EventRecords;

    public UpdateTaskCommandHandler(
        INetptuneUnitOfWork unitOfWork,
        IActivityLogger activity,
        IEventPublisher eventPublisher,
        IIdentityService identity,
        IEventRecordWriter eventRecords)
    {
        UnitOfWork = unitOfWork;
        Activity = activity;
        EventPublisher = eventPublisher;
        Identity = identity;
        EventRecords = eventRecords;
    }

    public async ValueTask<ClientResponse<TaskViewModel>> Handle(UpdateTaskCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;
        var old = await UnitOfWork.Tasks.GetTaskViewModel(req.Id, cancellationToken);

        if (old is null)
        {
            return ClientResponse<TaskViewModel>.NotFound;
        }

        var result = await UnitOfWork.Tasks.GetTaskForUpdate(req.Id, cancellationToken);

        if (result is null)
        {
            return ClientResponse<TaskViewModel>.NotFound;
        }

        TaskViewModel? response = null;
        ProjectTaskDiff? diff = null;

        await UnitOfWork.Transaction(async () =>
        {
            var status = await ResolveStatus(req, result.WorkspaceId, cancellationToken);

            if (status is not null && result.StatusId != status.Id)
            {
                result.StatusId = status.Id;
            }

            result.Name = req.Name ?? result.Name;
            result.Description = req.Description ?? result.Description;
            result.OwnerId = req.OwnerId ?? result.OwnerId;
            result.Priority = req.Priority ?? result.Priority;
            result.EstimateType = req.EstimateType ?? result.EstimateType;
            result.EstimateValue = req.EstimateValue ?? result.EstimateValue;

            if (req.DueDateSpecified)
            {
                result.DueDate = req.DueDate;
            }

            if (req.AssigneeIds is not null)
            {
                result.ProjectTaskAppUsers = ProjectTaskAppUser.MergeUsersIds(
                    result.Id,
                    result.ProjectTaskAppUsers,
                    req.AssigneeIds).ToList();
            }

            await UnitOfWork.CompleteAsync(cancellationToken);

            response = await UnitOfWork.Tasks.GetTaskViewModel(result.Id, cancellationToken);

            if (response is null)
            {
                return;
            }

            diff = ProjectTaskDiff.Create(old, response);

            await AppendReportingEvents(
                old,
                response,
                diff,
                cancellationToken);
            await UnitOfWork.CompleteAsync(cancellationToken);
        });

        if (response is null)
        {
            return ClientResponse<TaskViewModel>.NotFound;
        }

        if (diff is null)
        {
            return ClientResponse<TaskViewModel>.Success(response);
        }

        diff.LogDiff(Activity, response.Id);

        if (diff.HasChanges && response.WorkspaceId is not null)
        {
            await PublishTaskChanged(response, diff);
        }

        return ClientResponse<TaskViewModel>.Success(response);
    }

    private async Task AppendReportingEvents(
        TaskViewModel old,
        TaskViewModel updated,
        ProjectTaskDiff diff,
        CancellationToken cancellationToken)
    {
        foreach (var change in diff.ToTaskFieldChanges())
        {
            var payload = new FieldTransitionedPayload
            {
                Field = change.Field.ToString().ToLowerInvariant(),
                OldValue = change.OldValue,
                NewValue = change.NewValue,
                OldCategory = change.Field == TaskChangeField.Status ? old.StatusCategory.ToString() : null,
                NewCategory = change.Field == TaskChangeField.Status ? updated.StatusCategory.ToString() : null,
                OldUnit = change.Field == TaskChangeField.Estimate ? old.EstimateType?.ToString() : null,
                NewUnit = change.Field == TaskChangeField.Estimate ? updated.EstimateType?.ToString() : null,
                OldNumericValue = change.Field == TaskChangeField.Estimate ? old.EstimateValue : null,
                NewNumericValue = change.Field == TaskChangeField.Estimate ? updated.EstimateValue : null,
            };

            var references = new List<EventReferenceInput>();
            var projectId = updated.ProjectId ?? old.ProjectId;

            if (projectId.HasValue)
            {
                references.Add(new(
                    EventReferenceRoles.Scope,
                    EventEntityTypes.From(EntityType.Project),
                    projectId.Value.ToString()));
            }

            if (updated.SprintId.HasValue)
            {
                references.Add(new EventReferenceInput(
                    EventReferenceRoles.Scope,
                    EventEntityTypes.From(EntityType.Sprint),
                    updated.SprintId.Value.ToString()));
            }

            await EventRecords.Append(new EventWriteRequest<FieldTransitionedPayload>
            {
                WorkspaceId = updated.WorkspaceId,
                EventKey = EventKeys.EntityFieldTransitioned,
                SubjectType = EventEntityTypes.From(EntityType.Task),
                SubjectId = updated.Id.ToString(),
                Payload = payload,
                References = references,
            }, cancellationToken);

            if (change.Field == TaskChangeField.Estimate && updated.SprintId.HasValue && updated.SprintStatus == SprintStatus.Active)
            {
                await EventRecords.Append(new EventWriteRequest<ScopeMemberAttributeChangedPayload>
                {
                    WorkspaceId = updated.WorkspaceId,
                    EventKey = EventKeys.ScopeMemberAttributeChanged,
                    SubjectType = EventEntityTypes.From(EntityType.Sprint),
                    SubjectId = updated.SprintId.Value.ToString(),
                    Payload = new ScopeMemberAttributeChangedPayload
                    {
                        MemberType = EventEntityTypes.From(EntityType.Task),
                        MemberId = updated.Id.ToString(),
                        Field = "estimate",
                        OldUnit = old.EstimateType?.ToString(),
                        NewUnit = updated.EstimateType?.ToString(),
                        OldNumericValue = old.EstimateValue,
                        NewNumericValue = updated.EstimateValue,
                    },
                    References =
                    [
                        new EventReferenceInput(EventReferenceRoles.Member, EventEntityTypes.From(EntityType.Task), updated.Id.ToString()),
                        ..references,
                    ],
                }, cancellationToken);
            }
        }
    }

    private Task PublishTaskChanged(TaskViewModel current, ProjectTaskDiff diff)
    {
        return EventPublisher.Dispatch(new TaskChangedMessage
        {
            WorkspaceId = current.WorkspaceId!.Value,
            TaskId = current.Id,
            ActorUserId = Identity.GetCurrentUserId(),
            Changes = diff.ToTaskFieldChanges(),
        });
    }

    private async Task<Status?> ResolveStatus(UpdateProjectTaskRequest request, int workspaceId, CancellationToken cancellationToken)
    {

        if (request.StatusId.HasValue)
        {
            var status = await UnitOfWork.Statuses.GetInWorkspace(request.StatusId.Value, workspaceId, cancellationToken: cancellationToken);

            return status;
        }

        return null;
    }
}
