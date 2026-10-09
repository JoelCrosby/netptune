using Mediator;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Events;
using Netptune.Core.Events.Sprints;
using Netptune.Core.Models.Search;
using Netptune.Core.Models.Sprints;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Sprints;

namespace Netptune.Handlers.Sprints.Commands;

public sealed record UpdateSprintCommand(UpdateSprintRequest Request) : IRequest<ClientResponse<SprintViewModel>>;

public sealed class UpdateSprintCommandHandler : IRequestHandler<UpdateSprintCommand, ClientResponse<SprintViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IActivityLogger Activity;
    private readonly IEventPublisher EventPublisher;
    private readonly IEventRecordWriter EventRecords;
    private readonly IMediator Mediator;

    public UpdateSprintCommandHandler(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        IActivityLogger activity,
        IEventPublisher eventPublisher,
        IEventRecordWriter eventRecords,
        IMediator mediator)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Activity = activity;
        EventPublisher = eventPublisher;
        EventRecords = eventRecords;
        Mediator = mediator;
    }

    public async ValueTask<ClientResponse<SprintViewModel>> Handle(UpdateSprintCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;
        var workspaceKey = Identity.GetWorkspaceKey();
        var sprint = await UnitOfWork.Sprints.GetSprintInWorkspaceAsync(workspaceKey, req.Id, cancellationToken: cancellationToken);

        if (sprint is null) return ClientResponse<SprintViewModel>.NotFound;
        if (sprint.Status == SprintStatus.Completed && req.Status != SprintStatus.Cancelled)
        {
            return ClientResponse<SprintViewModel>.Failed("Completed sprints cannot be edited");
        }

        // Completion records the sprint's commitment for reporting and carries unfinished tasks off,
        // so a status change to Completed is handed to it rather than just flipping the field.
        var isCompleting = req.Status == SprintStatus.Completed && sprint.Status != SprintStatus.Completed;

        if (isCompleting && sprint.Status != SprintStatus.Active)
        {
            return ClientResponse<SprintViewModel>.Failed("Only active sprints can be completed");
        }

        if (!string.IsNullOrWhiteSpace(req.Name))
        {
            sprint.Name = req.Name.Trim();
        }

        sprint.Goal = req.Goal ?? sprint.Goal;
        sprint.StartDate = req.StartDate ?? sprint.StartDate;
        sprint.EndDate = req.EndDate ?? sprint.EndDate;

        if (sprint.EndDate < sprint.StartDate)
        {
            return ClientResponse<SprintViewModel>.Failed("Sprint end date must be after start date");
        }

        var wasActive = sprint.Status == SprintStatus.Active;
        var isCancelling = req.Status == SprintStatus.Cancelled && sprint.Status != SprintStatus.Cancelled;

        if (isCompleting)
        {
            var hasFieldEdits = !string.IsNullOrWhiteSpace(req.Name)
                || req.Goal is not null
                || req.StartDate.HasValue
                || req.EndDate.HasValue;

            return await CompleteWithEdits(sprint.Id, hasFieldEdits, cancellationToken);
        }

        if (req.Status.HasValue)
        {
            sprint.Status = req.Status.Value;
            sprint.CompletedAt = req.Status.Value == SprintStatus.Completed
                ? DateTime.UtcNow
                : sprint.CompletedAt;
        }

        var user = await Identity.GetCurrentUser();

        sprint.ModifiedByUserId = user.Id;

        // A cancelled sprint hands its unfinished tasks back to the backlog, so they are never left on
        // a sprint nobody will work from again.
        var unfinishedTasks = isCancelling ? GetUnfinishedTasks(sprint) : [];

        await UnitOfWork.Transaction(async () =>
        {
            await ReleaseUnfinishedTasks(sprint, unfinishedTasks, wasActive, cancellationToken);
            await UnitOfWork.CompleteAsync(cancellationToken);
        });

        var result = await UnitOfWork.Sprints.GetSprintDetailAsync(workspaceKey, sprint.Id, cancellationToken);

        Activity.Log(options =>
        {
            options.EntityId = sprint.Id;
            options.EntityType = EntityType.Sprint;
            options.Type = ActivityType.Modify;
        });

        await EventPublisher.IndexSprints([sprint.Id], workspaceKey);

        return result is null
            ? ClientResponse<SprintViewModel>.NotFound
            : ClientResponse<SprintViewModel>.Success(result);
    }

    private async Task<ClientResponse<SprintViewModel>> CompleteWithEdits(
        int sprintId,
        bool hasFieldEdits,
        CancellationToken cancellationToken)
    {
        // The field edits are still unsaved on the tracked sprint. Completion loads that same instance
        // from the shared unit of work and saves it in its own transaction, so the edits commit with
        // the completion or not at all.
        var completion = await Mediator.Send(new CompleteSprintCommand(sprintId), cancellationToken);

        if (completion.IsSuccess && hasFieldEdits)
        {
            Activity.Log(options =>
            {
                options.EntityId = sprintId;
                options.EntityType = EntityType.Sprint;
                options.Type = ActivityType.Modify;
            });
        }

        return completion;
    }

    private async Task ReleaseUnfinishedTasks(
        Sprint sprint,
        List<ProjectTask> unfinishedTasks,
        bool wasActive,
        CancellationToken cancellationToken)
    {
        var taskIds = unfinishedTasks.ConvertAll(task => task.Id);

        await UnitOfWork.Tasks.RemoveTasksFromSprint(taskIds, cancellationToken);

        if (!wasActive)
        {
            return;
        }

        var scope = new SprintScope(sprint.WorkspaceId, sprint.Id, sprint.ProjectId);
        var removedEvents = unfinishedTasks.ConvertAll(task =>
        {
            return SprintMemberEvents.Changed(scope, ToSprintMember(task), SprintMemberChanges.Removed);
        });

        await EventRecords.AppendRange(removedEvents, cancellationToken);
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

    private static List<ProjectTask> GetUnfinishedTasks(Sprint sprint)
    {
        return sprint.ProjectTasks
            .Where(task => !task.IsDeleted && task.Status!.Category != StatusCategory.Done)
            .GroupBy(task => task.Id)
            .Select(group => group.First())
            .ToList();
    }
}
