using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Netptune.Automation.Configuration;
using Netptune.Automation.Diagnostics;
using Netptune.Automation.Models;
using Netptune.Core.Enums;
using Netptune.Core.Models.Automations;
using Netptune.Core.UnitOfWork;

namespace Netptune.Automation.Execution;

internal sealed class AutomationLimitGuard
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly LimitsOptions Options;
    private readonly ILogger<AutomationLimitGuard> Logger;

    public AutomationLimitGuard(
        INetptuneUnitOfWork unitOfWork,
        IOptions<LimitsOptions> options,
        ILogger<AutomationLimitGuard> logger)
    {
        UnitOfWork = unitOfWork;
        Options = options.Value;
        Logger = logger;
    }

    public async Task<List<PendingAutomationExecution>> Filter(
        AutomationTriggerType triggerType,
        List<PendingAutomationExecution> executions,
        CancellationToken cancellationToken)
    {
        if (executions.Count == 0)
        {
            return executions;
        }

        var withinQuota = await FilterByWorkspaceQuota(triggerType, executions, cancellationToken);

        if (!Options.CircuitBreakerEnabled || withinQuota.Count == 0)
        {
            return withinQuota;
        }

        var ruleIds = withinQuota.Select(execution => execution.Rule.Id).Distinct().ToList();
        var since = DateTime.UtcNow.Subtract(Options.Window);
        var stats = await UnitOfWork.Automations.GetRunStats(ruleIds, since, cancellationToken);
        var trippedRules = stats
            .Select(BuildTrip)
            .Where(trip => trip is not null)
            .Select(trip => trip!)
            .ToList();

        if (trippedRules.Count == 0)
        {
            return withinQuota;
        }

        var trippedRuleIds = trippedRules.Select(trip => trip.RuleId).ToHashSet();

        foreach (var trip in trippedRules)
        {
            await DisableRule(triggerType, trip, cancellationToken);
        }

        var skippedCount = withinQuota.Count(execution => trippedRuleIds.Contains(execution.Rule.Id));

        Telemetry.RecordRunsSkipped(triggerType, skippedCount, "circuit_breaker");

        return withinQuota
            .Where(execution => !trippedRuleIds.Contains(execution.Rule.Id))
            .ToList();
    }

    private async Task<List<PendingAutomationExecution>> FilterByWorkspaceQuota(
        AutomationTriggerType triggerType,
        List<PendingAutomationExecution> executions,
        CancellationToken cancellationToken)
    {
        var quota = Options.WorkspaceRunQuota;

        if (quota == 0)
        {
            return executions;
        }

        var since = DateTime.UtcNow.Subtract(Options.Window);
        var workspaceIds = executions.Select(execution => execution.Rule.WorkspaceId).Distinct().ToList();
        var exhaustedWorkspaceIds = new HashSet<int>();

        foreach (var workspaceId in workspaceIds)
        {
            var runCount = await UnitOfWork.Automations.GetWorkspaceRunCount(workspaceId, since, cancellationToken);
            var quotaExhausted = runCount >= quota;

            if (!quotaExhausted)
            {
                continue;
            }

            exhaustedWorkspaceIds.Add(workspaceId);

            Logger.LogWarning(
                "Workspace {WorkspaceId} reached its automation run quota of {Quota} while handling {TriggerType}",
                workspaceId,
                quota,
                triggerType);
        }

        if (exhaustedWorkspaceIds.Count == 0)
        {
            return executions;
        }

        var allowed = executions
            .Where(execution => !exhaustedWorkspaceIds.Contains(execution.Rule.WorkspaceId))
            .ToList();

        Telemetry.RecordRunsSkipped(triggerType, executions.Count - allowed.Count, "workspace_quota");

        return allowed;
    }

    private CircuitBreakerTrip? BuildTrip(AutomationRunStats stats)
    {
        var exceededFailures = stats.FailureCount >= Options.FailureThreshold;

        if (exceededFailures)
        {
            return new CircuitBreakerTrip(
                stats.RuleId,
                $"Disabled automatically after {stats.FailureCount} failed runs within {DescribeWindow()}.");
        }

        var exceededRuns = stats.RunCount >= Options.RunThreshold;

        if (exceededRuns)
        {
            return new CircuitBreakerTrip(
                stats.RuleId,
                $"Disabled automatically after {stats.RunCount} runs within {DescribeWindow()}.");
        }

        return null;
    }

    private async Task DisableRule(
        AutomationTriggerType triggerType,
        CircuitBreakerTrip trip,
        CancellationToken cancellationToken)
    {
        await UnitOfWork.Automations.AutoDisableRules([trip.RuleId], trip.Reason, DateTime.UtcNow, cancellationToken);

        Logger.LogWarning(
            "Automation rule {RuleId} was disabled by the circuit breaker while handling {TriggerType}: {Reason}",
            trip.RuleId,
            triggerType,
            trip.Reason);
    }

    private string DescribeWindow()
    {
        var window = Options.Window;

        if (window.TotalHours < 1)
        {
            return $"{Math.Round(window.TotalMinutes)} minutes";
        }

        var tenthsOfAnHour = (int)Math.Round(window.TotalHours * 10);

        if (tenthsOfAnHour == 10)
        {
            return "an hour";
        }

        return $"{tenthsOfAnHour / 10d} hours";
    }
}

internal sealed record CircuitBreakerTrip(int RuleId, string Reason);
