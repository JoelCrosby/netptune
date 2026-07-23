using System.Diagnostics;

using Microsoft.Extensions.Logging;

using Netptune.Automation.Diagnostics;
using Netptune.Automation.Models;
using Netptune.Core.Encoding;
using Netptune.Core.Enums;
using Netptune.Core.UnitOfWork;

namespace Netptune.Automation.Matching;

internal sealed class DueDateAutomationRuleMatcher
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly ILogger<DueDateAutomationRuleMatcher> Logger;

    public DueDateAutomationRuleMatcher(
        INetptuneUnitOfWork unitOfWork,
        ILogger<DueDateAutomationRuleMatcher> logger)
    {
        UnitOfWork = unitOfWork;
        Logger = logger;
    }

    internal async Task<List<PendingAutomationExecution>> Match(CancellationToken cancellationToken)
    {
        var activity = Activity.Current;
        const AutomationTriggerType triggerType = AutomationTriggerType.TaskDueDateApproaching;

        Logger.LogInformation("Evaluating scheduled due-date automation rules");

        var rules = await UnitOfWork.Automations.GetEnabledRulesForTrigger(
            triggerType,
            cancellationToken: cancellationToken);

        Telemetry.RecordRulesEvaluated(triggerType, rules.Count);
        activity?.SetTag("automation.rules.evaluated", rules.Count);

        var rulesWithDurations = rules
            .Select(rule =>
            {
                var durationDays = JsonUtils.ReadInt(rule.TriggerConfig, "durationDays");

                return new { Rule = rule, DurationDays = durationDays };
            })
            .ToList();

        var validRulesWithDurations = rulesWithDurations
            .Where(rule => rule.DurationDays is >= 0 and <= 365)
            .ToList();

        var ruleDefinitions = validRulesWithDurations
            .Select(rule => new DueDateRuleDefinition(rule.Rule, rule.DurationDays.GetValueOrDefault()))
            .ToList();

        var invalidRuleCount = rules.Count - ruleDefinitions.Count;

        if (invalidRuleCount > 0)
        {
            Logger.LogWarning(
                "Skipped {InvalidRuleCount} due-date automation rules with missing or invalid durationDays",
                invalidRuleCount);
            Telemetry.RecordRulesSkipped(triggerType, invalidRuleCount, "invalid_config");
        }

        if (ruleDefinitions.Count == 0)
        {
            Logger.LogDebug("No configured due-date automation rules were eligible for evaluation");
            return [];
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var workspaceIds = ruleDefinitions.Select(rule => rule.Rule.WorkspaceId).Distinct().ToList();
        var latestDueDate = today.AddDays(ruleDefinitions.Max(rule => rule.DurationDays));
        var tasks = await UnitOfWork.Tasks.GetDueDateAutomationCandidates(
            workspaceIds,
            today,
            latestDueDate,
            cancellationToken);

        activity?.SetTag("automation.rules.configured", ruleDefinitions.Count);
        activity?.SetTag("automation.workspaces.evaluated", workspaceIds.Count);
        activity?.SetTag("automation.tasks.candidate", tasks.Count);

        var rulesByWorkspace = ruleDefinitions
            .GroupBy(rule => rule.Rule.WorkspaceId)
            .ToDictionary(group => group.Key, group => group.ToList());

        var executions = new List<PendingAutomationExecution>();

        foreach (var task in tasks)
        {
            if (task.DueDate is not { } dueDate ||
                !rulesByWorkspace.TryGetValue(task.WorkspaceId, out var workspaceRules))
            {
                continue;
            }

            foreach (var rule in workspaceRules)
            {
                if (dueDate != today.AddDays(rule.DurationDays))
                {
                    continue;
                }

                var actorUserId = rule.Rule.OwnerId ?? rule.Rule.CreatedByUserId ?? task.OwnerId;

                if (actorUserId is null)
                {
                    Logger.LogWarning("Automation rule {RuleId} skipped task {TaskId}: no actor user id", rule.Rule.Id, task.Id);
                    Telemetry.RecordRunsSkipped(triggerType, 1, "missing_actor");

                    continue;
                }

                executions.Add(new PendingAutomationExecution
                {
                    Rule = rule.Rule,
                    Task = task,
                    ActorUserId = actorUserId,
                    IdempotencyKey = $"rule:{rule.Rule.Id}:task:{task.Id}:due:{dueDate:yyyy-MM-dd}",
                    TriggeredAt = DateTime.UtcNow,
                });
            }
        }

        Telemetry.RecordRulesMatched(triggerType, executions.Count);
        activity?.SetTag("automation.rules.matched", executions.Count);

        Logger.LogInformation(
            "Matched {MatchedRuleCount} scheduled due-date automation executions from {CandidateTaskCount} candidate tasks",
            executions.Count,
            tasks.Count);

        return executions;
    }
}
