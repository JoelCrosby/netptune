using System.Diagnostics;

using Microsoft.Extensions.Logging;

using Netptune.Automation.Common;
using Netptune.Automation.Diagnostics;
using Netptune.Automation.Models;
using Netptune.Core.Encoding;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Models.Automations;
using Netptune.Core.UnitOfWork;

namespace Netptune.Automation.Matching;

internal sealed class DueDateRuleMatcher : IScheduledRuleMatcher
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly ILogger<DueDateRuleMatcher> Logger;

    public AutomationTriggerType TriggerType => AutomationTriggerType.TaskDueDateApproaching;

    public DueDateRuleMatcher(
        INetptuneUnitOfWork unitOfWork,
        ILogger<DueDateRuleMatcher> logger)
    {
        UnitOfWork = unitOfWork;
        Logger = logger;
    }

    public async Task<List<PendingAutomationExecution>> Match(CancellationToken cancellationToken)
    {
        var activity = Activity.Current;

        Logger.LogInformation("Evaluating scheduled due-date automation rules");

        var rules = await UnitOfWork.Automations.GetEnabledRulesForTrigger(
            TriggerType,
            cancellationToken: cancellationToken);

        Telemetry.RecordRulesEvaluated(TriggerType, rules.Count);
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
            Telemetry.RecordRulesSkipped(TriggerType, invalidRuleCount, "invalid_config");
        }

        if (ruleDefinitions.Count == 0)
        {
            Logger.LogDebug("No configured due-date automation rules were eligible for evaluation");

            return [];
        }

        var now = DateTime.UtcNow;
        var localDates = ruleDefinitions
            .Select(rule => AutomationTimeZones.Today(rule.Rule, now))
            .ToList();
        var workspaceIds = ruleDefinitions.Select(rule => rule.Rule.WorkspaceId).Distinct().ToList();
        var earliestDueDate = localDates.Min();
        var latestDueDate = localDates.Max().AddDays(ruleDefinitions.Max(rule => rule.DurationDays));
        var tasks = await UnitOfWork.Tasks.GetDueDateAutomationCandidates(
            workspaceIds,
            earliestDueDate,
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
                var matchesDueDate = AutomationTriggerPredicates.MatchesDueDateApproaching(rule.Rule, task, now);
                var matchesConditions = AutomationRuleConditions.Match(rule.Rule, task);

                if (!matchesDueDate || !matchesConditions)
                {
                    continue;
                }

                executions.Add(new PendingAutomationExecution
                {
                    Rule = rule.Rule,
                    Task = task,
                    ExecutionUserId = rule.Rule.ExecutionUserId,
                    IdempotencyKey = $"rule:{rule.Rule.Id}:task:{task.Id}:due:{dueDate:yyyy-MM-dd}",
                    TriggeredAt = now,
                });
            }
        }

        Telemetry.RecordRulesMatched(TriggerType, executions.Count);
        activity?.SetTag("automation.rules.matched", executions.Count);

        Logger.LogInformation(
            "Matched {MatchedRuleCount} scheduled due-date automation executions from {CandidateTaskCount} candidate tasks",
            executions.Count,
            tasks.Count);

        return executions;
    }
}
