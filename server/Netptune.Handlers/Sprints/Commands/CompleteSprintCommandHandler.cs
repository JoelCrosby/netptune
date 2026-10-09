using Mediator;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Events;
using Netptune.Core.Events.Sprints;
using Netptune.Core.Models.Search;
using Netptune.Core.Models.Sprints;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Sprints;

namespace Netptune.Handlers.Sprints.Commands;

public sealed record CompleteSprintCommand(int Id, int? CarryOverSprintId = null) : IRequest<ClientResponse<SprintViewModel>>;

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

        var carryOverTarget = request.CarryOverSprintId.HasValue
            ? await UnitOfWork.Sprints.GetTaskAssignmentTarget(workspaceKey, request.CarryOverSprintId.Value, cancellationToken)
            : null;

        if (request.CarryOverSprintId.HasValue)
        {
            var carryOverError = DescribeInvalidCarryOverTarget(sprint, carryOverTarget);

            if (carryOverError is not null)
            {
                return ClientResponse<SprintViewModel>.Failed(carryOverError);
            }
        }

        var user = await Identity.GetCurrentUser();
        var completedAt = DateTime.UtcNow;
        var members = GetSprintMembers(sprint);
        var unfinishedTasks = members
            .Where(task => !task.IsDeleted && task.Status!.Category != StatusCategory.Done)
            .ToList();

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
                    Commitment = members
                        .Select(task => new SprintCommitmentMember
                        {
                            TaskId = task.Id,
                            StatusId = task.StatusId,
                            StatusCategory = task.Status!.Category.ToString(),
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

            // Unfinished work leaves with the sprint, inside the same transaction, so no caller can
            // close a sprint and strand its open tasks where neither the backlog nor a sprint shows them.
            // The commitment above already recorded them, so leaving here is not a scope change.
            await CarryOverUnfinishedTasks(unfinishedTasks, carryOverTarget, cancellationToken);

            await UnitOfWork.CompleteAsync(cancellationToken);
        });

        var result = await UnitOfWork.Sprints.GetSprintDetailAsync(workspaceKey, sprint.Id, cancellationToken);

        Activity.Log(options =>
        {
            options.EntityId = sprint.Id;
            options.EntityType = EntityType.Sprint;
            options.Type = ActivityType.ModifyStatus;
        });

        await EventPublisher.Dispatch(new SprintLifecycleMessage
        {
            WorkspaceId = sprint.WorkspaceId,
            SprintId = sprint.Id,
            State = SprintLifecycleState.Completed,
            ActorUserId = user.Id,
        });

        await EventPublisher.IndexSprints([sprint.Id], workspaceKey);

        return result is null
            ? ClientResponse<SprintViewModel>.NotFound
            : ClientResponse<SprintViewModel>.Success(result);
    }

    private async Task CarryOverUnfinishedTasks(
        List<ProjectTask> unfinishedTasks,
        SprintTaskAssignmentTarget? carryOverTarget,
        CancellationToken cancellationToken)
    {
        var taskIds = unfinishedTasks.ConvertAll(task => task.Id);

        if (carryOverTarget is null)
        {
            await UnitOfWork.Tasks.RemoveTasksFromSprint(taskIds, cancellationToken);

            return;
        }

        await UnitOfWork.Tasks.AssignTasksToSprint(taskIds, carryOverTarget.Id, cancellationToken);

        if (carryOverTarget.Status != SprintStatus.Active)
        {
            return;
        }

        var scope = new SprintScope(carryOverTarget.WorkspaceId, carryOverTarget.Id, carryOverTarget.ProjectId);
        var addedEvents = unfinishedTasks.ConvertAll(task =>
        {
            return SprintMemberEvents.Changed(scope, ToSprintMember(task), SprintMemberChanges.Added);
        });

        await EventRecords.AppendRange(addedEvents, cancellationToken);
    }

    private static SprintMember ToSprintMember(ProjectTask task)
    {
        return new SprintMember
        {
            TaskId = task.Id,
            StatusId = task.StatusId,
            StatusCategory = task.Status!.Category.ToString(),
            EstimateType = task.EstimateType?.ToString(),
            EstimateValue = task.EstimateValue,
        };
    }

    private static string? DescribeInvalidCarryOverTarget(Sprint sprint, SprintTaskAssignmentTarget? target)
    {
        if (target is null || target.ProjectId != sprint.ProjectId)
        {
            return "The sprint to carry unfinished tasks over to was not found in this project";
        }

        if (target.Id == sprint.Id)
        {
            return "Unfinished tasks cannot carry over to the sprint being completed";
        }

        if (target.Status is not (SprintStatus.Planning or SprintStatus.Active))
        {
            return "Unfinished tasks can only carry over to a planning or active sprint";
        }

        return null;
    }

    private static List<ProjectTask> GetSprintMembers(Sprint sprint)
    {
        return sprint.ProjectTasks
            .GroupBy(task => task.Id)
            .Select(group => group.First())
            .ToList();
    }
}
