using Microsoft.Extensions.Logging;

using Netptune.Automation.Configuration;
using Netptune.Automation.Diagnostics;
using Netptune.Automation.Models;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.UnitOfWork;

namespace Netptune.Automation.Planning;

internal sealed class FlagPlanner
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly ILogger<FlagPlanner> Logger;

    public FlagPlanner(
        INetptuneUnitOfWork unitOfWork,
        ILogger<FlagPlanner> logger)
    {
        UnitOfWork = unitOfWork;
        Logger = logger;
    }

    internal async Task<List<Flag>> BuildFlags(
        AutomationTriggerType triggerType,
        List<FlagPlan> flagPlans,
        CancellationToken cancellationToken)
    {
        if (flagPlans.Count == 0) return [];

        var ruleIds = flagPlans.Select(plan => plan.Execution.Rule.Id).Distinct().ToList();
        var taskIds = flagPlans.Select(plan => plan.Execution.Task.Id).Distinct().ToList();

        var existingFlags = await UnitOfWork.Flags.GetExistingAutomationTaskFlags(ruleIds, taskIds, cancellationToken);

        var existingSet = existingFlags
            .Select(CreateFlagKey)
            .ToHashSet();
        var distinctPlans = flagPlans
            .DistinctBy(CreateFlagKey)
            .ToList();
        var flags = distinctPlans
            .Where(plan => !existingSet.Contains(CreateFlagKey(plan)))
            .Select(plan =>
            {
                var rule = plan.Execution.Rule;
                var task = plan.Execution.Task;
                var action = plan.Action;

                return new Flag
                {
                    WorkspaceId = task.WorkspaceId,
                    EntityType = EntityType.Task,
                    EntityId = task.Id,
                    AutomationRuleId = rule.Id,
                    Name = ConfigReader.ReadString(action.Config, "flagName") ?? "Automation flag",
                    Description = ConfigReader.ReadString(action.Config, "flagDescription") ?? $"Flagged by automation '{rule.Name}'.",
                    OwnerId = plan.Execution.ActorUserId,
                    CreatedByUserId = plan.Execution.ActorUserId,
                };
            })
            .ToList();

        Telemetry.RecordFlagsCreated(triggerType, flags.Count);

        Logger.LogInformation(
            "Planned {FlagCount} automation flags for trigger {TriggerType}; skipped {ExistingFlagCount} existing flags and {DuplicateFlagPlanCount} duplicate plans",
            flags.Count,
            triggerType,
            existingSet.Count,
            flagPlans.Count - distinctPlans.Count);

        return flags;
    }

    private static FlagKey CreateFlagKey(FlagPlan plan)
    {
        return new FlagKey(plan.Execution.Rule.Id, plan.Execution.Task.Id);
    }

    private static FlagKey CreateFlagKey(Flag flag)
    {
        return new FlagKey(
            flag.AutomationRuleId.GetValueOrDefault(),
            flag.EntityId.GetValueOrDefault());
    }
}
