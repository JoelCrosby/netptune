using System.Diagnostics;
using System.Text.Json;

using Microsoft.Extensions.Logging;

using Netptune.Automation.Models;
using Netptune.Core.Encoding;
using Netptune.Core.Enums;
using Netptune.Core.Models.Automations;
using Netptune.Core.Models.ProjectTasks;
using Netptune.Core.Services.ProjectTasks;
using Netptune.Core.UnitOfWork;

namespace Netptune.Automation.Persistence.Actions;

internal sealed class UpdateTaskHandler : IActionExecutionHandler
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly ITaskMutationPipeline TaskMutationPipeline;
    private readonly ILogger<UpdateTaskHandler> Logger;

    public UpdateTaskHandler(
        INetptuneUnitOfWork unitOfWork,
        ITaskMutationPipeline taskMutationPipeline,
        ILogger<UpdateTaskHandler> logger)
    {
        UnitOfWork = unitOfWork;
        TaskMutationPipeline = taskMutationPipeline;
        Logger = logger;
    }

    public AutomationActionType Type => AutomationActionType.UpdateTask;

    public async Task<ActionOutcome> Execute(
        PlannedAutomationAction action,
        AutomationPersistenceState state,
        CancellationToken cancellationToken)
    {
        var contribution = action.Contribution.TaskUpdate;

        if (contribution is null)
        {
            return ActionOutcomes.InvalidContribution();
        }

        var taskUpdated = await ApplyTaskUpdate(action, contribution, state.TaskMutations, cancellationToken);

        if (!taskUpdated)
        {
            return new ActionOutcome(
                AutomationActionResultStatus.Skipped,
                "The task already has the configured values.");
        }

        return ActionOutcomes.Succeeded();
    }

    private async Task<bool> ApplyTaskUpdate(
        PlannedAutomationAction action,
        AutomationTaskUpdateContribution contribution,
        List<TaskMutationOutcome> taskMutations,
        CancellationToken cancellationToken)
    {
        var execution = action.Execution;
        var taskId = execution.Task.Id;
        var previous = await UnitOfWork.Tasks.GetTaskViewModel(taskId, cancellationToken);
        var task = await UnitOfWork.Tasks.GetTaskForUpdate(taskId, cancellationToken);

        if (previous is null || task is null)
        {
            Logger.LogWarning(
                "Automation rule {RuleId} could not update missing or deleted task {TaskId}",
                execution.Rule.Id,
                taskId);

            return false;
        }

        var status = contribution.StatusId.HasValue
            ? await UnitOfWork.Statuses.GetInWorkspace(
                contribution.StatusId.Value,
                execution.Rule.WorkspaceId,
                cancellationToken: cancellationToken)
            : null;
        var taskUpdated = TaskMutationPipeline.Apply(task, new TaskMutationValues(status, contribution.Priority));

        if (!taskUpdated)
        {
            return false;
        }

        execution.Task.StatusId = task.StatusId;
        execution.Task.Status = task.Status;
        execution.Task.Priority = task.Priority;
        task.UpdatedAt = DateTime.UtcNow;
        task.ModifiedByUserId = execution.ExecutionUserId!;

        await UnitOfWork.CompleteAsync(cancellationToken);

        var current = await UnitOfWork.Tasks.GetTaskViewModel(taskId, cancellationToken);

        if (current is null)
        {
            Logger.LogWarning(
                "Automation rule {RuleId} could not record the update for task {TaskId}",
                execution.Rule.Id,
                taskId);

            return false;
        }

        var diff = ProjectTaskDiff.Create(previous, current);
        var nextChainDepth = execution.ChainDepth + 1;
        var outcome = await TaskMutationPipeline.Record(new TaskMutationRequest
        {
            Previous = previous,
            Current = current,
            Diff = diff,
            ActorUserId = execution.ExecutionUserId!,
            OriginType = EventOriginType.Automation,
            CorrelationId = execution.CorrelationId,
            CausationEventId = execution.CausationEventId,
            AutomationRuleId = execution.Rule.Id,
            AutomationRunId = execution.Run?.Id,
            ChainDepth = nextChainDepth,
        }, cancellationToken);

        taskMutations.Add(outcome);
        action.Result.Output = JsonSerializer.SerializeToDocument(new
        {
            changedFields = diff.ChangedFields.Select(field => field.ToString()).ToList(),
        }, JsonOptions.Default);
        Activity.Current?.AddTag("automation.task_update.applied", taskId);

        return true;
    }
}
