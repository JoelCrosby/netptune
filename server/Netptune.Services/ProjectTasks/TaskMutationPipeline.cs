using Netptune.Core.Enums;
using Netptune.Core.Entities;
using Netptune.Core.Events;
using Netptune.Core.Events.Tasks;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.Services.ProjectTasks;
using Netptune.Core.ViewModels.ProjectTasks;

namespace Netptune.Services.ProjectTasks;

public sealed class TaskMutationPipeline : ITaskMutationPipeline
{
    private readonly IEventRecordWriter EventRecords;
    private readonly IEventPublisher EventPublisher;
    private readonly IActivityLogger? Activity;

    public TaskMutationPipeline(
        IEventRecordWriter eventRecords,
        IEventPublisher eventPublisher,
        IActivityLogger? activity = null)
    {
        EventRecords = eventRecords;
        EventPublisher = eventPublisher;
        Activity = activity;
    }

    public bool Apply(ProjectTask task, TaskMutationValues values)
    {
        var statusChanged = values.Status is not null && task.StatusId != values.Status.Id;

        if (statusChanged)
        {
            var status = values.Status!;

            task.StatusId = status.Id;
            task.Status = status;
        }

        var priority = values.Priority;
        var priorityChanged = priority.HasValue && task.Priority != priority.Value;

        if (priorityChanged)
        {
            task.Priority = priority;
        }

        var taskChanged = statusChanged || priorityChanged;

        return taskChanged;
    }

    public async Task<TaskMutationOutcome> Record(
        TaskMutationRequest request,
        CancellationToken cancellationToken = default)
    {
        var changes = request.Diff.ToTaskFieldChanges();

        foreach (var change in changes)
        {
            await AppendReportingEvent(
                request.Previous,
                request.Current,
                request.ActorUserId,
                change,
                cancellationToken);
        }

        var message = request.Current.WorkspaceId.HasValue
            ? new TaskChangedMessage
            {
                WorkspaceId = request.Current.WorkspaceId.Value,
                TaskId = request.Current.Id,
                ActorUserId = request.ActorUserId,
                Changes = changes,
            }
            : null;

        return new TaskMutationOutcome(request, message);
    }

    public async Task Publish(TaskMutationOutcome outcome)
    {
        var mutation = outcome.Mutation;
        var current = mutation.Current;

        if (Activity is not null)
        {
            mutation.Diff.LogDiff(Activity, current.Id, current.WorkspaceId, mutation.ActorUserId);
        }

        var hasChanges = mutation.Diff.HasChanges;
        var hasMessage = outcome.Message is not null;
        var shouldPublish = hasChanges && hasMessage;

        if (shouldPublish)
        {
            await EventPublisher.Dispatch(outcome.Message!);
        }
    }

    private async Task AppendReportingEvent(
        TaskViewModel previous,
        TaskViewModel current,
        string actorUserId,
        TaskFieldChange change,
        CancellationToken cancellationToken)
    {
        var payload = new FieldTransitionedPayload
        {
            Field = change.Field.ToString().ToLowerInvariant(),
            OldValue = change.OldValue,
            NewValue = change.NewValue,
            OldCategory = change.Field == TaskChangeField.Status ? previous.StatusCategory.ToString() : null,
            NewCategory = change.Field == TaskChangeField.Status ? current.StatusCategory.ToString() : null,
            OldUnit = change.Field == TaskChangeField.Estimate ? previous.EstimateType?.ToString() : null,
            NewUnit = change.Field == TaskChangeField.Estimate ? current.EstimateType?.ToString() : null,
            OldNumericValue = change.Field == TaskChangeField.Estimate ? previous.EstimateValue : null,
            NewNumericValue = change.Field == TaskChangeField.Estimate ? current.EstimateValue : null,
        };
        var references = BuildScopeReferences(previous, current);

        await EventRecords.Append(new EventWriteRequest<FieldTransitionedPayload>
        {
            WorkspaceId = current.WorkspaceId,
            EventKey = EventKeys.EntityFieldTransitioned,
            SubjectType = EventEntityTypes.From(EntityType.Task),
            SubjectId = current.Id.ToString(),
            ActorUserId = actorUserId,
            Payload = payload,
            References = references,
        }, cancellationToken);

        var estimateChangedInActiveSprint = change.Field == TaskChangeField.Estimate
            && current.SprintId.HasValue
            && current.SprintStatus == SprintStatus.Active;

        if (estimateChangedInActiveSprint)
        {
            await AppendSprintEstimateEvent(previous, current, actorUserId, references, cancellationToken);
        }
    }

    private async Task AppendSprintEstimateEvent(
        TaskViewModel previous,
        TaskViewModel current,
        string actorUserId,
        List<EventReferenceInput> references,
        CancellationToken cancellationToken)
    {
        await EventRecords.Append(new EventWriteRequest<ScopeMemberAttributeChangedPayload>
        {
            WorkspaceId = current.WorkspaceId,
            EventKey = EventKeys.ScopeMemberAttributeChanged,
            SubjectType = EventEntityTypes.From(EntityType.Sprint),
            SubjectId = current.SprintId!.Value.ToString(),
            ActorUserId = actorUserId,
            Payload = new ScopeMemberAttributeChangedPayload
            {
                MemberType = EventEntityTypes.From(EntityType.Task),
                MemberId = current.Id.ToString(),
                Field = "estimate",
                OldUnit = previous.EstimateType?.ToString(),
                NewUnit = current.EstimateType?.ToString(),
                OldNumericValue = previous.EstimateValue,
                NewNumericValue = current.EstimateValue,
            },
            References =
            [
                new EventReferenceInput
                {
                    Role = EventReferenceRoles.Member,
                    EntityType = EventEntityTypes.From(EntityType.Task),
                    EntityId = current.Id.ToString(),
                },
                ..references,
            ],
        }, cancellationToken);
    }

    private static List<EventReferenceInput> BuildScopeReferences(
        TaskViewModel previous,
        TaskViewModel current)
    {
        var references = new List<EventReferenceInput>();
        var projectId = current.ProjectId ?? previous.ProjectId;

        if (projectId.HasValue)
        {
            references.Add(new EventReferenceInput
            {
                Role = EventReferenceRoles.Scope,
                EntityType = EventEntityTypes.From(EntityType.Project),
                EntityId = projectId.Value.ToString(),
            });
        }

        if (current.SprintId.HasValue)
        {
            references.Add(new EventReferenceInput
            {
                Role = EventReferenceRoles.Scope,
                EntityType = EventEntityTypes.From(EntityType.Sprint),
                EntityId = current.SprintId.Value.ToString(),
            });
        }

        return references;
    }
}
