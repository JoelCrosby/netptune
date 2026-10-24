using System.Text.Json;

using Netptune.Automation.Common;
using Netptune.Automation.Models;
using Netptune.Core.Encoding;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Events;
using Netptune.Core.Models.Automations;
using Netptune.Core.Relations;
using Netptune.Core.Relationships;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;

namespace Netptune.Automation.Persistence.Actions;

internal sealed class ManageTaskRelationHandler : IActionExecutionHandler
{
    private const string RelationAdded = "added";
    private const string RelationRemoved = "removed";

    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IEventRecordWriter EventRecords;

    public ManageTaskRelationHandler(INetptuneUnitOfWork unitOfWork, IEventRecordWriter eventRecords)
    {
        UnitOfWork = unitOfWork;
        EventRecords = eventRecords;
    }

    public AutomationActionType Type => AutomationActionType.ManageTaskRelation;

    public async Task<ActionOutcome> Execute(
        PlannedAutomationAction action,
        AutomationPersistenceState state,
        CancellationToken cancellationToken)
    {
        var contribution = action.Contribution.Relation;

        if (contribution is null)
        {
            return ActionOutcomes.InvalidContribution();
        }

        var workspaceId = action.Execution.Rule.WorkspaceId;
        var relationType = await UnitOfWork.RelationTypes.GetInWorkspace(
            contribution.RelationTypeId,
            workspaceId,
            cancellationToken: cancellationToken);

        if (relationType is null)
        {
            return new ActionOutcome(
                AutomationActionResultStatus.Failed,
                AutomationMessage.RelationTypeUnavailable);
        }

        if (contribution.Operation == AutomationRelationOperation.Remove)
        {
            return await RemoveRelations(action, contribution, cancellationToken);
        }

        return await AddRelation(action, contribution, relationType, cancellationToken);
    }

    private async Task<ActionOutcome> AddRelation(
        PlannedAutomationAction action,
        AutomationRelationContribution contribution,
        RelationType relationType,
        CancellationToken cancellationToken)
    {
        var execution = action.Execution;
        var workspaceId = execution.Rule.WorkspaceId;
        var taskId = execution.Task.Id;

        if (!contribution.RelatedTaskId.HasValue)
        {
            return new ActionOutcome(AutomationActionResultStatus.Failed, AutomationMessage.RelationTargetMissing);
        }

        var relatedTaskId = contribution.RelatedTaskId.Value;

        if (relatedTaskId == taskId)
        {
            return new ActionOutcome(AutomationActionResultStatus.Failed, AutomationMessage.RelationToSelf);
        }

        var validTaskIds = await UnitOfWork.Tasks.GetValidTaskIdsInWorkspace(
            [relatedTaskId],
            workspaceId,
            cancellationToken);
        var relatedTaskExists = validTaskIds.Contains(relatedTaskId);

        if (!relatedTaskExists)
        {
            return new ActionOutcome(
                AutomationActionResultStatus.Failed,
                AutomationMessage.RelationTargetUnavailable);
        }

        var (sourceTaskId, targetTaskId) = Orient(relationType.Category, contribution.Direction, taskId, relatedTaskId);
        var alreadyLinked = await UnitOfWork.ProjectTaskRelations.Exists(
            relationType.Id,
            sourceTaskId,
            targetTaskId,
            cancellationToken);

        if (alreadyLinked)
        {
            return new ActionOutcome(
                AutomationActionResultStatus.Skipped,
                AutomationMessage.RelationAlreadyLinked);
        }

        var conflictError = await FindLinkConflict(relationType, sourceTaskId, targetTaskId, cancellationToken);

        if (conflictError is not null)
        {
            return new ActionOutcome(AutomationActionResultStatus.Failed, conflictError);
        }

        var relation = new ProjectTaskRelation
        {
            WorkspaceId = workspaceId,
            RelationTypeId = relationType.Id,
            SourceTaskId = sourceTaskId,
            TargetTaskId = targetTaskId,
        };

        var relationEvent = BuildRelationEvent(action, relation, RelationAdded);

        await UnitOfWork.ProjectTaskRelations.AddAsync(relation, cancellationToken);
        await EventRecords.Append(relationEvent, cancellationToken);
        await UnitOfWork.CompleteAsync(cancellationToken);

        action.Result.Output = JsonSerializer.SerializeToDocument(new
        {
            relationId = relation.Id,
            relatedTaskId,
        }, JsonOptions.Default);

        return ActionOutcomes.Succeeded();
    }

    private async Task<ActionOutcome> RemoveRelations(
        PlannedAutomationAction action,
        AutomationRelationContribution contribution,
        CancellationToken cancellationToken)
    {
        var taskId = action.Execution.Task.Id;
        var relations = await UnitOfWork.ProjectTaskRelations.GetForTaskAndType(
            contribution.RelationTypeId,
            taskId,
            contribution.RelatedTaskId,
            cancellationToken);

        if (relations.Count == 0)
        {
            return new ActionOutcome(
                AutomationActionResultStatus.Skipped,
                AutomationMessage.RelationNothingToRemove);
        }

        var relationIds = relations.Select(relation => relation.Id).ToList();
        var relationEvents = relations
            .Select(relation => (IEventWriteRequest)BuildRelationEvent(action, relation, RelationRemoved))
            .ToList();

        await UnitOfWork.ProjectTaskRelations.DeletePermanent(relationIds, cancellationToken);
        await EventRecords.AppendRange(relationEvents, cancellationToken);
        await UnitOfWork.CompleteAsync(cancellationToken);

        action.Result.Output = JsonSerializer.SerializeToDocument(new
        {
            removedRelationCount = relationIds.Count,
        }, JsonOptions.Default);

        return ActionOutcomes.Succeeded();
    }

    private static EventWriteRequest<TaskRelationChangedPayload> BuildRelationEvent(
        PlannedAutomationAction action,
        ProjectTaskRelation relation,
        string change)
    {
        var execution = action.Execution;
        var task = execution.Task;
        var relatedTaskId = relation.SourceTaskId == task.Id ? relation.TargetTaskId : relation.SourceTaskId;

        return new EventWriteRequest<TaskRelationChangedPayload>
        {
            WorkspaceId = task.WorkspaceId,
            EventKey = EventKeys.TaskRelationChanged,
            SubjectType = EventEntityTypes.From(EntityType.Task),
            SubjectId = task.Id.ToString(),
            ActorUserId = execution.ExecutionUserId!,
            OriginType = EventOriginType.Automation,
            CorrelationId = execution.CorrelationId,
            CausationEventId = execution.CausationEventId,
            Payload = new TaskRelationChangedPayload
            {
                Change = change,
                RelationTypeId = relation.RelationTypeId,
                SourceTaskId = relation.SourceTaskId,
                TargetTaskId = relation.TargetTaskId,
            },
            References =
            [
                new EventReferenceInput
                {
                    Role = EventReferenceRoles.Member,
                    EntityType = EventEntityTypes.From(EntityType.Task),
                    EntityId = relatedTaskId.ToString(),
                },
            ],
        };
    }

    private async Task<AutomationMessage?> FindLinkConflict(
        RelationType relationType,
        int sourceTaskId,
        int targetTaskId,
        CancellationToken cancellationToken)
    {
        var hasSingleSource = RelationTypeRules.HasSingleSource(relationType.Category);

        if (hasSingleSource)
        {
            var hasExistingSource = await UnitOfWork.ProjectTaskRelations.HasExistingSource(
                relationType.Id,
                targetTaskId,
                cancellationToken);

            if (hasExistingSource)
            {
                return AutomationMessage.RelationSingleSource(relationType.Name);
            }
        }

        var isAcyclic = RelationTypeRules.IsAcyclic(relationType.Category);

        if (isAcyclic)
        {
            var wouldCreateCycle = await UnitOfWork.ProjectTaskRelations.WouldCreateCycle(
                relationType.Id,
                sourceTaskId,
                targetTaskId,
                cancellationToken);

            if (wouldCreateCycle)
            {
                return AutomationMessage.RelationCycle(relationType.Name);
            }
        }

        return null;
    }

    private static (int SourceTaskId, int TargetTaskId) Orient(
        RelationCategory category,
        AutomationRelationDirection direction,
        int taskId,
        int relatedTaskId)
    {
        var isSymmetric = RelationTypeRules.IsSymmetric(category);

        if (isSymmetric)
        {
            return taskId < relatedTaskId ? (taskId, relatedTaskId) : (relatedTaskId, taskId);
        }

        var taskIsSource = direction == AutomationRelationDirection.TaskIsSource;

        return taskIsSource ? (taskId, relatedTaskId) : (relatedTaskId, taskId);
    }
}
