using Mediator;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Events;
using Netptune.Core.Models.Search;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Sprints;

namespace Netptune.Handlers.Sprints.Commands;

public sealed record CompleteSprintCommand(int Id) : IRequest<ClientResponse<SprintViewModel>>;

public sealed class CompleteSprintCommandHandler : IRequestHandler<CompleteSprintCommand, ClientResponse<SprintViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IActivityLogger Activity;
    private readonly IEventPublisher EventPublisher;
    private readonly IEventRecordWriter EventRecords;

    public CompleteSprintCommandHandler(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        IActivityLogger activity,
        IEventPublisher eventPublisher,
        IEventRecordWriter eventRecords)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Activity = activity;
        EventPublisher = eventPublisher;
        EventRecords = eventRecords;
    }

    public async ValueTask<ClientResponse<SprintViewModel>> Handle(CompleteSprintCommand request, CancellationToken cancellationToken)
    {
        var workspaceKey = Identity.GetWorkspaceKey();
        var sprint = await UnitOfWork.Sprints.GetSprintInWorkspaceAsync(workspaceKey, request.Id, cancellationToken: cancellationToken);

        if (sprint is null)
        {
            return ClientResponse<SprintViewModel>.NotFound;
        }

        if (sprint.Status != SprintStatus.Active)
        {
            return ClientResponse<SprintViewModel>.Failed("Only active sprints can be completed");
        }

        var user = await Identity.GetCurrentUser();
        var completedAt = DateTime.UtcNow;

        await UnitOfWork.Transaction(async () =>
        {
            sprint.Status = SprintStatus.Completed;
            sprint.CompletedAt = completedAt;
            sprint.ModifiedByUserId = user.Id;

            await EventRecords.Append(new EventWriteRequest<ScopeLifecyclePayload>
            {
                WorkspaceId = sprint.WorkspaceId,
                EventKey = EventKeys.ScopeLifecycleTransitioned,
                SubjectType = EventEntityTypes.From(EntityType.Sprint),
                SubjectId = sprint.Id.ToString(),
                OccurredAt = completedAt,
                Payload = new ScopeLifecyclePayload
                {
                    State = "completed",
                    PlannedStart = sprint.StartDate,
                    PlannedEnd = sprint.EndDate,
                    ActualStart = sprint.StartedAt,
                    CompletedAt = completedAt,
                    Commitment = sprint.ProjectTasks
                        .Where(task => !task.IsDeleted)
                        .Select(task => new SprintCommitmentMember
                        {
                            TaskId = task.Id,
                            StatusId = task.StatusId,
                            StatusCategory = task.Status.Category.ToString(),
                            EstimateType = task.EstimateType?.ToString(),
                            EstimateValue = task.EstimateValue,
                        })
                        .ToList(),
                },
                References =
                [
                    new EventReferenceInput(
                        EventReferenceRoles.Scope,
                        EventEntityTypes.From(EntityType.Project),
                        sprint.ProjectId.ToString()),
                ],
            }, cancellationToken);

            await UnitOfWork.CompleteAsync(cancellationToken);
        });

        var result = await UnitOfWork.Sprints.GetSprintDetailAsync(workspaceKey, sprint.Id, cancellationToken);

        Activity.Log(options =>
        {
            options.EntityId = sprint.Id;
            options.EntityType = EntityType.Sprint;
            options.Type = ActivityType.ModifyStatus;
        });

        await EventPublisher.Dispatch(new SearchIndexEvent
        {
            Operation = SearchIndexOperation.Index,
            EntityType = "sprint",
            EntityIds = [sprint.Id],
            WorkspaceSlug = workspaceKey,
        });

        return result is null
            ? ClientResponse<SprintViewModel>.NotFound
            : ClientResponse<SprintViewModel>.Success(result);
    }
}
