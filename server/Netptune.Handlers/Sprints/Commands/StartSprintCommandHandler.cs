using Mediator;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Models.Search;
using Netptune.Core.Events;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Sprints;

namespace Netptune.Handlers.Sprints.Commands;

public sealed record StartSprintCommand(int Id) : IRequest<ClientResponse<SprintViewModel>>;

public sealed class StartSprintCommandHandler : IRequestHandler<StartSprintCommand, ClientResponse<SprintViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IActivityLogger Activity;
    private readonly IEventPublisher EventPublisher;
    private readonly IEventRecordWriter EventRecords;

    public StartSprintCommandHandler(
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

    public async ValueTask<ClientResponse<SprintViewModel>> Handle(StartSprintCommand request, CancellationToken cancellationToken)
    {
        var workspaceKey = Identity.GetWorkspaceKey();
        var sprint = await UnitOfWork.Sprints.GetSprintInWorkspaceAsync(workspaceKey, request.Id, cancellationToken: cancellationToken);

        if (sprint is null)
        {
            return ClientResponse<SprintViewModel>.NotFound;
        }

        if (sprint.Status != SprintStatus.Planning)
        {
            return ClientResponse<SprintViewModel>.Failed("Only planning sprints can be started");
        }

        var hasActiveSprint = await UnitOfWork.Sprints.HasActiveSprintAsync(sprint.ProjectId, sprint.Id, cancellationToken);

        if (hasActiveSprint)
        {
            return ClientResponse<SprintViewModel>.Failed("This project already has an active sprint");
        }

        var user = await Identity.GetCurrentUser();
        var startedAt = DateTime.UtcNow;

        var committedTasks = GetUniqueSprintTasks(sprint);

        await UnitOfWork.Transaction(async () =>
        {
            sprint.Status = SprintStatus.Active;
            sprint.StartedAt = startedAt;
            sprint.ModifiedByUserId = user.Id;

            await EventRecords.Append(new EventWriteRequest<ScopeLifecyclePayload>
            {
                WorkspaceId = sprint.WorkspaceId,
                EventKey = EventKeys.ScopeLifecycleTransitioned,
                SubjectType = EventEntityTypes.From(EntityType.Sprint),
                SubjectId = sprint.Id.ToString(),
                OccurredAt = startedAt,
                Payload = new ScopeLifecyclePayload
                {
                    State = "started",
                    PlannedStart = sprint.StartDate,
                    PlannedEnd = sprint.EndDate,
                    ActualStart = startedAt,
                    Commitment = committedTasks
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
                    new EventReferenceInput
                    {
                        Role = EventReferenceRoles.Scope,
                        EntityType = EventEntityTypes.From(EntityType.Project),
                        EntityId = sprint.ProjectId.ToString(),
                    },
                ],
            }, cancellationToken);

            foreach (var task in committedTasks)
            {
                await EventRecords.Append(new EventWriteRequest<ScopeMemberChangedPayload>
                {
                    WorkspaceId = sprint.WorkspaceId,
                    EventKey = EventKeys.ScopeMemberChanged,
                    SubjectType = EventEntityTypes.From(EntityType.Sprint),
                    SubjectId = sprint.Id.ToString(),
                    OccurredAt = startedAt,
                    Payload = new ScopeMemberChangedPayload
                    {
                        Change = "committed",
                        MemberType = EventEntityTypes.From(EntityType.Task),
                        MemberId = task.Id.ToString(),
                        EstimateType = task.EstimateType?.ToString(),
                        EstimateValue = task.EstimateValue,
                        StatusId = task.StatusId,
                        StatusCategory = task.Status.Category.ToString(),
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

    // The sprint's task graph can contain the same task more than once; collapsing to unique tasks
    // keeps the committed snapshot and per-member events free of duplicates that would otherwise
    // break burndown replay downstream.
    private static List<ProjectTask> GetUniqueSprintTasks(Sprint sprint)
    {
        return sprint.ProjectTasks
            .Where(task => !task.IsDeleted)
            .GroupBy(task => task.Id)
            .Select(group => group.First())
            .ToList();
    }
}
