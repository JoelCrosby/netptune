using System.Diagnostics;

using Microsoft.Extensions.Logging;

using Netptune.Automation.Diagnostics;
using Netptune.Automation.Matching;
using Netptune.Core.Enums;
using Netptune.Core.Events.Tasks;

namespace Netptune.Automation.Execution;

public interface IExecutionService
{
    Task ExecuteStatusChangedRules(TaskStatusChangedMessage message, CancellationToken cancellationToken);

    Task ExecuteUnassignedRules(CancellationToken cancellationToken);
}

internal sealed class ExecutionService : IExecutionService
{
    private readonly StatusChangedAutomationRuleMatcher StatusChangedMatcher;
    private readonly UnassignedTaskAutomationRuleMatcher UnassignedTaskMatcher;
    private readonly RuleExecutor RuleExecutor;
    private readonly ILogger<ExecutionService> Logger;

    public ExecutionService(
        StatusChangedAutomationRuleMatcher statusChangedMatcher,
        UnassignedTaskAutomationRuleMatcher unassignedTaskMatcher,
        RuleExecutor ruleExecutor,
        ILogger<ExecutionService> logger)
    {
        StatusChangedMatcher = statusChangedMatcher;
        UnassignedTaskMatcher = unassignedTaskMatcher;
        RuleExecutor = ruleExecutor;
        Logger = logger;
    }

    public async Task ExecuteStatusChangedRules(TaskStatusChangedMessage message, CancellationToken cancellationToken)
    {
        using var activity = Telemetry.StartActivity(
            "automation.execute_status_changed_rules",
            AutomationTriggerType.TaskStatusChanged);
        var startedAt = Stopwatch.GetTimestamp();

        activity?.SetTag("task.id", message.TaskId);
        activity?.SetTag("workspace.id", message.WorkspaceId);
        activity?.SetTag("automation.event_id", message.EventId.ToString());
        activity?.SetTag("automation.old_status", message.OldStatus.ToString());
        activity?.SetTag("automation.new_status", message.NewStatus.ToString());

        try
        {
            var executions = await StatusChangedMatcher.Match(message, cancellationToken);

            await RuleExecutor.Execute(AutomationTriggerType.TaskStatusChanged, executions, cancellationToken);
        }
        catch (Exception ex)
        {
            Telemetry.MarkFailed(activity, ex);
            Logger.LogError(
                ex,
                "Task status automation execution failed for task {TaskId} in workspace {WorkspaceId}",
                message.TaskId,
                message.WorkspaceId);
            throw;
        }
        finally
        {
            Telemetry.RecordExecutionDuration(
                AutomationTriggerType.TaskStatusChanged,
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
}
