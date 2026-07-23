using Microsoft.Extensions.Logging;

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

    public FlagPlanner(INetptuneUnitOfWork unitOfWork, ILogger<FlagPlanner> logger)
    {
        UnitOfWork = unitOfWork;
        Logger = logger;
    }

    internal async Task<Dictionary<PlannedAutomationAction, Flag>> BuildFlags(
        AutomationTriggerType triggerType,
        List<PlannedAutomationAction> actions,
        CancellationToken cancellationToken)
    {
        var flagActions = actions
            .Where(action => action.Contribution.Flag is not null)
            .ToList();

        if (flagActions.Count == 0)
        {
            return new Dictionary<PlannedAutomationAction, Flag>();
        }

        var ruleIds = flagActions.Select(action => action.Execution.Rule.Id).Distinct().ToList();
        var taskIds = flagActions.Select(action => action.Execution.Task.Id).Distinct().ToList();

        var existingFlags = await UnitOfWork.Flags.GetExistingAutomationTaskFlags(ruleIds, taskIds, cancellationToken);

        var existingSet = existingFlags
            .Select(CreateFlagKey)
            .ToHashSet();

        var distinctActions = flagActions
            .DistinctBy(CreateFlagKey)
            .ToList();

        var plannedFlags = distinctActions
            .Where(action => !existingSet.Contains(CreateFlagKey(action)))
            .ToDictionary(action => action, action =>
            {
                var rule = action.Execution.Rule;
                var task = action.Execution.Task;
                var contribution = action.Contribution.Flag!;

                return new Flag
                {
                    WorkspaceId = task.WorkspaceId,
                    EntityType = EntityType.Task,
                    EntityId = task.Id,
                    AutomationRuleId = rule.Id,
                    Name = contribution.Name,
                    Description = contribution.Description,
                    OwnerId = action.Execution.ExecutionUserId!,
                    CreatedByUserId = action.Execution.ExecutionUserId!,
                };
            });

        Telemetry.RecordFlagsCreated(triggerType, plannedFlags.Count);

        Logger.LogInformation(
            "Planned {FlagCount} automation flags for trigger {TriggerType}; skipped {ExistingFlagCount} existing flags and {DuplicateFlagPlanCount} duplicate plans",
            plannedFlags.Count,
            triggerType,
            existingSet.Count,
            flagActions.Count - distinctActions.Count);

        return plannedFlags;
    }

    private static FlagKey CreateFlagKey(PlannedAutomationAction action)
    {
        return new FlagKey(action.Execution.Rule.Id, action.Execution.Task.Id);
    }

    private static FlagKey CreateFlagKey(Flag flag)
    {
        return new FlagKey(
            flag.AutomationRuleId.GetValueOrDefault(),
            flag.EntityId.GetValueOrDefault());
    }
}
