using System.Diagnostics;

using Microsoft.Extensions.Logging;

using Netptune.Automation.Diagnostics;
using Netptune.Automation.Matching;
using Netptune.Core.Enums;
using Netptune.Core.Events.Tasks;

namespace Netptune.Automation.Execution;

public interface IExecutionService
{
    Task ExecuteTaskChangedRules(TaskChangedMessage message, CancellationToken cancellationToken);

    Task ExecuteUnassignedRules(CancellationToken cancellationToken);

    Task ExecuteDueDateRules(CancellationToken cancellationToken);

    Task ExecuteScheduledActions(CancellationToken cancellationToken);
}

internal sealed class ExecutionService : IExecutionService
{
    private readonly TaskChangedAutomationRuleMatcher TaskChangedMatcher;
    private readonly UnassignedTaskAutomationRuleMatcher UnassignedTaskMatcher;
    private readonly DueDateAutomationRuleMatcher DueDateMatcher;
    private readonly RuleExecutor RuleExecutor;
    private readonly ScheduledActionService ScheduledActions;
    private readonly ILogger<ExecutionService> Logger;

    public ExecutionService(
        TaskChangedAutomationRuleMatcher taskChangedMatcher,
        UnassignedTaskAutomationRuleMatcher unassignedTaskMatcher,
        DueDateAutomationRuleMatcher dueDateMatcher,
        RuleExecutor ruleExecutor,
        ScheduledActionService scheduledActions,
        ILogger<ExecutionService> logger)
    {
        TaskChangedMatcher = taskChangedMatcher;
        UnassignedTaskMatcher = unassignedTaskMatcher;
        DueDateMatcher = dueDateMatcher;
        RuleExecutor = ruleExecutor;
        ScheduledActions = scheduledActions;
        Logger = logger;
    }

    public async Task ExecuteTaskChangedRules(TaskChangedMessage message, CancellationToken cancellationToken)
    {
        using var activity = Telemetry.StartActivity(
            "automation.execute_task_changed_rules",
            AutomationTriggerType.TaskChanged);
        var startedAt = Stopwatch.GetTimestamp();

        activity?.SetTag("task.id", message.TaskId);
        activity?.SetTag("workspace.id", message.WorkspaceId);
        activity?.SetTag("automation.event_id", message.EventId.ToString());
        activity?.SetTag("automation.changed_fields", string.Join(",", message.Changes.Select(change => change.Field)));

        try
        {
            await ScheduledActions.CancelForStatusChange(message, cancellationToken);

            var executions = await TaskChangedMatcher.Match(message, cancellationToken);

            await RuleExecutor.Execute(AutomationTriggerType.TaskChanged, executions, cancellationToken);
        }
        catch (Exception ex)
        {
            Telemetry.MarkFailed(activity, ex);
            Logger.LogError(
                ex,
                "Task-change automation execution failed for task {TaskId} in workspace {WorkspaceId}",
                message.TaskId,
                message.WorkspaceId);
            throw;
        }
        finally
        {
            Telemetry.RecordExecutionDuration(
                AutomationTriggerType.TaskChanged,
                Stopwatch.GetElapsedTime(startedAt));
        }
    }

    public async Task ExecuteUnassignedRules(CancellationToken cancellationToken)
    {
        using var activity = Telemetry.StartActivity(
            "automation.execute_unassigned_rules",
            AutomationTriggerType.TaskUnassignedFor);
        var startedAt = Stopwatch.GetTimestamp();

        try
        {
            var executions = await UnassignedTaskMatcher.Match(cancellationToken);

            await RuleExecutor.Execute(AutomationTriggerType.TaskUnassignedFor, executions, cancellationToken);
        }
        catch (Exception ex)
        {
            Telemetry.MarkFailed(activity, ex);
            Logger.LogError(ex, "Scheduled unassigned-task automation execution failed");
            throw;
        }
        finally
        {
            Telemetry.RecordExecutionDuration(
                AutomationTriggerType.TaskUnassignedFor,
                Stopwatch.GetElapsedTime(startedAt));
        }
    }

    public async Task ExecuteDueDateRules(CancellationToken cancellationToken)
    {
        using var activity = Telemetry.StartActivity(
            "automation.execute_due_date_rules",
            AutomationTriggerType.TaskDueDateApproaching);
        var startedAt = Stopwatch.GetTimestamp();

        try
        {
            var executions = await DueDateMatcher.Match(cancellationToken);

            await RuleExecutor.Execute(AutomationTriggerType.TaskDueDateApproaching, executions, cancellationToken);
        }
        catch (Exception ex)
        {
            Telemetry.MarkFailed(activity, ex);
            Logger.LogError(ex, "Scheduled due-date automation execution failed");
            throw;
        }
        finally
        {
            Telemetry.RecordExecutionDuration(
                AutomationTriggerType.TaskDueDateApproaching,
                Stopwatch.GetElapsedTime(startedAt));
        }
    }

    public Task ExecuteScheduledActions(CancellationToken cancellationToken)
    {
        return ScheduledActions.ExecuteDue(cancellationToken);
    }
}
