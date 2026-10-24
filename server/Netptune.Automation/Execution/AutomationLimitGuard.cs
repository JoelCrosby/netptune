using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Netptune.Automation.Configuration;
using Netptune.Automation.Diagnostics;
using Netptune.Automation.Models;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Events;
using Netptune.Core.Models.Activity;
using Netptune.Core.Models.Automations;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;

namespace Netptune.Automation.Execution;

internal sealed class AutomationLimitGuard
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IEventRecordWriter EventRecords;
    private readonly LimitsOptions Options;
    private readonly ILogger<AutomationLimitGuard> Logger;

    public AutomationLimitGuard(
        INetptuneUnitOfWork unitOfWork,
        IEventRecordWriter eventRecords,
        IOptions<LimitsOptions> options,
        ILogger<AutomationLimitGuard> logger)
    {
        UnitOfWork = unitOfWork;
        EventRecords = eventRecords;
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
        var disabledRules = withinQuota
            .Select(execution => execution.Rule)
            .Where(rule => trippedRuleIds.Contains(rule.Id))
            .DistinctBy(rule => rule.Id)
            .ToList();

        await DisableRules(trippedRules, disabledRules, cancellationToken);

        foreach (var trip in trippedRules)
        {
            Logger.LogWarning(
                "Automation rule {RuleId} was disabled by the circuit breaker while handling {TriggerType}: {Reason}",
                trip.RuleId,
                triggerType,
                trip.Reason);
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

    private async Task DisableRules(
        List<CircuitBreakerTrip> trips,
        List<AutomationRule> rules,
        CancellationToken cancellationToken)
    {
        var reasonsByRuleId = trips.ToDictionary(trip => trip.RuleId, trip => trip.Reason);
        var ruleIds = reasonsByRuleId.Keys.ToList();

        await UnitOfWork.Transaction(async () =>
        {
            await UnitOfWork.Automations.AutoDisableRules(reasonsByRuleId, DateTime.UtcNow, cancellationToken);

            var cancelledCounts = await UnitOfWork.Automations.CancelPendingRuleActions(ruleIds, cancellationToken);
            var disabledEvents = rules
                .Where(rule => rule.ExecutionUserId is not null)
                .Select(rule =>
                {
                    var reason = reasonsByRuleId[rule.Id];
                    var cancelledCount = cancelledCounts.GetValueOrDefault(rule.Id);

                    return BuildAutoDisabledEvent(rule, reason, cancelledCount);
                })
                .ToList<IEventWriteRequest>();

            await EventRecords.AppendRange(disabledEvents, cancellationToken);
            await UnitOfWork.CompleteAsync(cancellationToken);
        });
    }

    private static EventWriteRequest<AutomationRuleActivityMeta> BuildAutoDisabledEvent(
        AutomationRule rule,
        string reason,
        int cancelledCount)
    {
        return new EventWriteRequest<AutomationRuleActivityMeta>
        {
            WorkspaceId = rule.WorkspaceId,
            EventKey = EventKeys.AutomationRuleAutoDisabled,
            SubjectType = EventEntityTypes.From(EntityType.AutomationRule),
            SubjectId = rule.Id.ToString(),
            ActorUserId = rule.ExecutionUserId,
            ResolveActorFromIdentity = false,
            OriginType = EventOriginType.System,
            Payload = new AutomationRuleActivityMeta
            {
                Name = rule.Name,
                Reason = reason,
                CancelledScheduledActions = AutomationRuleActivityMeta.CancelledCount(cancelledCount),
            },
        };
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
