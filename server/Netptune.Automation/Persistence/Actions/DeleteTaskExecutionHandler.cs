using System.Diagnostics;
using System.Text.Json;

using Netptune.Automation.Models;
using Netptune.Core.Encoding;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.UnitOfWork;

namespace Netptune.Automation.Persistence.Actions;

internal sealed class DeleteTaskExecutionHandler : IAutomationActionExecutionHandler
{
    private readonly INetptuneUnitOfWork UnitOfWork;

    public DeleteTaskExecutionHandler(INetptuneUnitOfWork unitOfWork)
    {
        UnitOfWork = unitOfWork;
    }

    public AutomationActionType Type => AutomationActionType.DeleteTask;

    public async Task<AutomationActionExecutionOutcome> Execute(
        PlannedAutomationAction action,
        AutomationPersistenceState state,
        CancellationToken cancellationToken)
    {
        var contribution = action.Contribution.TaskDeletion;

        if (contribution is null)
        {
            return AutomationActionExecutionOutcomes.InvalidContribution();
        }

        if (contribution.Delay > TimeSpan.Zero)
        {
            var scheduledAction = BuildScheduledAction(action, contribution.Delay);

            await UnitOfWork.Automations.AddScheduledActionsAsync([scheduledAction], cancellationToken);

            action.Result.Output = JsonSerializer.SerializeToDocument(new
            {
                executeAt = scheduledAction.ExecuteAt,
            }, JsonOptions.Default);

            return new AutomationActionExecutionOutcome(
                AutomationActionResultStatus.Scheduled,
                "The action was scheduled for later execution.");
        }

        var execution = action.Execution;
        var affected = await UnitOfWork.Tasks.SoftDelete(
            execution.Task.Id,
            execution.ActorUserId,
            cancellationToken);

        if (affected == 0)
        {
            return new AutomationActionExecutionOutcome(
                AutomationActionResultStatus.Skipped,
                "The task was already deleted.");
        }

        state.AppliedTaskDeletions.Add(action);
        state.DeletedTaskIds.Add(execution.Task.Id);

        Activity.Current?.AddTag("automation.task_deletion.applied", execution.Task.Id);

        return AutomationActionExecutionOutcomes.Succeeded();
    }

    private static ScheduledAutomationAction BuildScheduledAction(PlannedAutomationAction action, TimeSpan delay)
    {
        var execution = action.Execution;

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
            OwnerId = execution.ActorUserId,
            CreatedByUserId = execution.ActorUserId,
        };
    }
}
