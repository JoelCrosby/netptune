using System.Diagnostics;

using Microsoft.Extensions.Logging;

using Netptune.Automation.Diagnostics;
using Netptune.Automation.Models;
using Netptune.Core.Encoding;
using Netptune.Core.Enums;
using Netptune.Core.UnitOfWork;

namespace Netptune.Automation.Matching;

internal sealed class UnassignedTaskRuleMatcher
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly ILogger<UnassignedTaskRuleMatcher> Logger;

    public UnassignedTaskRuleMatcher(
        INetptuneUnitOfWork unitOfWork,
        ILogger<UnassignedTaskRuleMatcher> logger)
    {
        UnitOfWork = unitOfWork;
        Logger = logger;
    }

    internal async Task<List<PendingAutomationExecution>> Match(CancellationToken cancellationToken)
    {
        var activity = Activity.Current;

        Logger.LogInformation("Evaluating scheduled unassigned-task automation rules");

        var rules = await UnitOfWork.Automations.GetEnabledRulesForTrigger(
            AutomationTriggerType.TaskUnassignedFor,
            cancellationToken: cancellationToken);

        Telemetry.RecordRulesEvaluated(AutomationTriggerType.TaskUnassignedFor, rules.Count);
        activity?.SetTag("automation.rules.evaluated", rules.Count);

        var rulesWithDurations = rules
            .Select(rule =>
            {
                var durationDays = JsonUtils.ReadInt(rule.TriggerConfig, "durationDays");

                return new { Rule = rule, DurationDays = durationDays };
            })
            .ToList();
        var validRulesWithDurations = rulesWithDurations
            .Where(rule => rule.DurationDays is >= 1)
            .ToList();
        var ruleDefinitions = validRulesWithDurations
            .Select(rule => new UnassignedRuleDefinition(rule.Rule, rule.DurationDays.GetValueOrDefault()))
            .ToList();

        var invalidRuleCount = rules.Count - ruleDefinitions.Count;
        if (invalidRuleCount > 0)
        {
            Logger.LogWarning(
                "Skipped {InvalidRuleCount} unassigned-task automation rules with missing or invalid durationDays",
                invalidRuleCount);
            Telemetry.RecordRulesSkipped(AutomationTriggerType.TaskUnassignedFor, invalidRuleCount, "invalid_config");
        }

        if (ruleDefinitions.Count == 0)
        {
            Logger.LogDebug("No configured unassigned-task automation rules were eligible for evaluation");
            return [];
        }

        var now = DateTime.UtcNow;
        var workspaceIds = ruleDefinitions.Select(rule => rule.Rule.WorkspaceId).Distinct().ToList();
        var broadestCutoff = now.AddDays(-ruleDefinitions.Min(rule => rule.DurationDays));

        var tasks = await UnitOfWork.Tasks.GetUnassignedAutomationCandidates(
            workspaceIds,
            broadestCutoff,
            cancellationToken);

        activity?.SetTag("automation.rules.configured", ruleDefinitions.Count);
        activity?.SetTag("automation.workspaces.evaluated", workspaceIds.Count);
        activity?.SetTag("automation.tasks.candidate", tasks.Count);

        Logger.LogInformation(
            "Found {CandidateTaskCount} candidate unassigned tasks across {WorkspaceCount} workspaces for {RuleCount} automation rules",
            tasks.Count,
            workspaceIds.Count,
            ruleDefinitions.Count);

        var rulesByWorkspace = ruleDefinitions
            .GroupBy(rule => rule.Rule.WorkspaceId)
            .ToDictionary(group => group.Key, group => group.ToList());

        var runDate = now.ToString("yyyy-MM-dd");
        var executions = new List<PendingAutomationExecution>();

        foreach (var task in tasks)
        {
            if (!rulesByWorkspace.TryGetValue(task.WorkspaceId, out var workspaceRules)) continue;

            var taskTimestamp = task.UpdatedAt ?? task.CreatedAt;

            foreach (var rule in workspaceRules)
            {
                if (taskTimestamp > now.AddDays(-rule.DurationDays)) continue;

                executions.Add(new PendingAutomationExecution
                {
                    Rule = rule.Rule,
                    Task = task,
                    ExecutionUserId = rule.Rule.ExecutionUserId,
                    IdempotencyKey = $"rule:{rule.Rule.Id}:task:{task.Id}:unassigned:{runDate}",
                    TriggeredAt = now,
                });
            }
        }

        Telemetry.RecordRulesMatched(AutomationTriggerType.TaskUnassignedFor, executions.Count);
        activity?.SetTag("automation.rules.matched", executions.Count);

        Logger.LogInformation(
            "Matched {MatchedRuleCount} scheduled unassigned-task automation executions from {CandidateTaskCount} candidate tasks",
            executions.Count,
            tasks.Count);

        return executions;
    }
}
