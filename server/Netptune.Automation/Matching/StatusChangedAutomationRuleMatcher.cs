using System.Diagnostics;

using Microsoft.Extensions.Logging;

using Netptune.Automation.Configuration;
using Netptune.Automation.Diagnostics;
using Netptune.Automation.Models;
using Netptune.Core.Enums;
using Netptune.Core.Events.Tasks;
using Netptune.Core.UnitOfWork;

namespace Netptune.Automation.Matching;

internal sealed class StatusChangedAutomationRuleMatcher
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly ILogger<StatusChangedAutomationRuleMatcher> Logger;

    public StatusChangedAutomationRuleMatcher(
        INetptuneUnitOfWork unitOfWork,
        ILogger<StatusChangedAutomationRuleMatcher> logger)
    {
        UnitOfWork = unitOfWork;
        Logger = logger;
    }

    internal async Task<List<PendingAutomationExecution>> Match(
        TaskStatusChangedMessage message,
        CancellationToken cancellationToken)
    {
        var activity = Activity.Current;

        Logger.LogInformation(
            "Evaluating task status automation rules for task {TaskId} in workspace {WorkspaceId}: {OldStatus} -> {NewStatus} ({EventId})",
            message.TaskId,
            message.WorkspaceId,
            message.OldStatus,
            message.NewStatus,
            message.EventId);

        var rules = await UnitOfWork.Automations.GetEnabledRulesForTrigger(
            AutomationTriggerType.TaskStatusChanged,
            message.WorkspaceId,
            cancellationToken);

        Telemetry.RecordRulesEvaluated(AutomationTriggerType.TaskStatusChanged, rules.Count);
        activity?.SetTag("automation.rules.evaluated", rules.Count);

        if (rules.Count == 0)
        {
            Logger.LogDebug(
                "No task status automation rules found for workspace {WorkspaceId}",
                message.WorkspaceId);
            return [];
        }

        var task = await UnitOfWork.Tasks.GetAutomationTask(message.TaskId, cancellationToken);

        if (task is null)
        {
            Logger.LogWarning(
                "Task status automation skipped for missing or deleted task {TaskId}",
                message.TaskId);
            activity?.SetTag("automation.skip_reason", "task_not_found");
            Telemetry.RecordRulesSkipped(AutomationTriggerType.TaskStatusChanged, rules.Count, "task_not_found");

            return [];
        }

        var executions = rules
            .Where(rule => ConfigReader.ReadEnum<ProjectTaskStatus>(rule.TriggerConfig, "status") == message.NewStatus)
            .Select(rule => new PendingAutomationExecution(
                rule,
                task,
                message.ActorUserId,
                $"rule:{rule.Id}:task:{message.TaskId}:status:{message.OldStatus}->{message.NewStatus}:event:{message.EventId}"))
            .ToList();

        Telemetry.RecordRulesMatched(AutomationTriggerType.TaskStatusChanged, executions.Count);
        activity?.SetTag("automation.rules.matched", executions.Count);

        Logger.LogInformation(
            "Matched {MatchedRuleCount} of {RuleCount} task status automation rules for task {TaskId}",
            executions.Count,
            rules.Count,
            message.TaskId);

        return executions;
    }
}
