using System.Diagnostics;
using System.Text.Json;

using Netptune.Automation.Models;
using Netptune.Core.Encoding;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.UnitOfWork;

namespace Netptune.Automation.Persistence.Actions;

internal sealed class DeleteTaskHandler : IActionExecutionHandler
{
    private readonly INetptuneUnitOfWork UnitOfWork;

    public DeleteTaskHandler(INetptuneUnitOfWork unitOfWork)
    {
        UnitOfWork = unitOfWork;
    }

    public AutomationActionType Type => AutomationActionType.DeleteTask;

    public async Task<ActionOutcome> Execute(
        PlannedAutomationAction action,
        AutomationPersistenceState state,
        CancellationToken cancellationToken)
    {
        var contribution = action.Contribution.TaskDeletion;

        if (contribution is null)
        {
            return ActionOutcomes.InvalidContribution();
        }

        if (contribution.Delay > TimeSpan.Zero)
        {
            var scheduledAction = BuildScheduledAction(action, contribution.Delay);

            await UnitOfWork.Automations.AddScheduledActionsAsync([scheduledAction], cancellationToken);

            action.Result.Output = JsonSerializer.SerializeToDocument(new
            {
                executeAt = scheduledAction.ExecuteAt,
            }, JsonOptions.Default);

            return new ActionOutcome(
                AutomationActionResultStatus.Scheduled,
                "The action was scheduled for later execution.");
        }

        var execution = action.Execution;
        var affected = await UnitOfWork.Tasks.SoftDelete(
            execution.Task.Id,
            execution.ExecutionUserId!,
            cancellationToken);

        if (affected == 0)
        {
            return new ActionOutcome(
                AutomationActionResultStatus.Skipped,
                "The task was already deleted.");
        }

        state.AppliedTaskDeletions.Add(action);
        state.DeletedTaskIds.Add(execution.Task.Id);

        Activity.Current?.AddTag("automation.task_deletion.applied", execution.Task.Id);

        return ActionOutcomes.Succeeded();
    }

    private static ScheduledAutomationAction BuildScheduledAction(PlannedAutomationAction action, TimeSpan delay)
    {
        var execution = action.Execution;
        var triggerContext = execution.TriggerMessage is null
            ? null
            : JsonSerializer.SerializeToDocument(execution.TriggerMessage, JsonOptions.Default);

        return new ScheduledAutomationAction
        {
            AutomationRuleId = execution.Rule.Id,
            AutomationActionId = action.Action.Id,
            TaskId = execution.Task.Id,
            ActionType = AutomationActionType.DeleteTask,
            Status = ScheduledAutomationActionStatus.Pending,
            ExpectedStatusId = execution.Task.StatusId,
            ExecuteAt = execution.TriggeredAt.Add(delay),
            IdempotencyKey = $"{execution.IdempotencyKey}:action:{action.Action.Id}",
            TriggerContext = triggerContext,
            OwnerId = execution.ExecutionUserId!,
            CreatedByUserId = execution.ExecutionUserId!,
        };
    }
}
