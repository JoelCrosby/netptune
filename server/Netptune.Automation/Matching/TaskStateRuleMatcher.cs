using System.Diagnostics;

using Microsoft.Extensions.Logging;

using Netptune.Automation.Common;
using Netptune.Automation.Diagnostics;
using Netptune.Automation.Models;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Models.Automations;
using Netptune.Core.UnitOfWork;

namespace Netptune.Automation.Matching;

internal abstract class TaskStateRuleMatcher : IScheduledRuleMatcher
{
    protected INetptuneUnitOfWork UnitOfWork { get; }

    protected ILogger Logger { get; }

    public abstract AutomationTriggerType TriggerType { get; }

    protected TaskStateRuleMatcher(INetptuneUnitOfWork unitOfWork, ILogger logger)
    {
        UnitOfWork = unitOfWork;
        Logger = logger;
    }

    public async Task<List<PendingAutomationExecution>> Match(CancellationToken cancellationToken)
    {
        var rules = await UnitOfWork.Automations.GetEnabledRulesForTrigger(
            TriggerType,
            cancellationToken: cancellationToken);

        Telemetry.RecordRulesEvaluated(TriggerType, rules.Count);
        Activity.Current?.SetTag("automation.rules.evaluated", rules.Count);

        if (rules.Count == 0)
        {
            return [];
        }

        var now = DateTime.UtcNow;
        var workspaceIds = rules.Select(rule => rule.WorkspaceId).Distinct().ToList();
        var tasks = await GetCandidates(rules, workspaceIds, now, cancellationToken);
        var rulesByWorkspace = rules
            .GroupBy(rule => rule.WorkspaceId)
            .ToDictionary(group => group.Key, group => group.ToList());
        var executions = new List<PendingAutomationExecution>();

        foreach (var task in tasks)
        {
            if (!rulesByWorkspace.TryGetValue(task.WorkspaceId, out var workspaceRules))
            {
                continue;
            }

            foreach (var rule in workspaceRules)
            {
                var matchesTrigger = MatchesTrigger(rule, task, now);
                var matchesConditions = AutomationRuleConditions.Match(rule, task);

                if (!matchesTrigger || !matchesConditions)
                {
                    continue;
                }

                executions.Add(CreateExecution(rule, task, now));
            }
        }

        Telemetry.RecordRulesMatched(TriggerType, executions.Count);
        Logger.LogInformation(
            "Matched {MatchedRuleCount} {TriggerType} automation executions from {CandidateTaskCount} candidates",
            executions.Count,
            TriggerType,
            tasks.Count);

        return executions;
    }

    protected abstract Task<List<ProjectTask>> GetCandidates(
        List<AutomationRule> rules,
        List<int> workspaceIds,
        DateTime now,
        CancellationToken cancellationToken);

    protected abstract bool MatchesTrigger(AutomationRule rule, ProjectTask task, DateTime now);

    protected abstract string GetStateKey(ProjectTask task);

    private PendingAutomationExecution CreateExecution(AutomationRule rule, ProjectTask task, DateTime now)
    {
        var stateKey = GetStateKey(task);

        return new PendingAutomationExecution
        {
            Rule = rule,
            Task = task,
            ExecutionUserId = rule.ExecutionUserId,
            IdempotencyKey = $"rule:{rule.Id}:task:{task.Id}:{stateKey}",
            TriggeredAt = now,
        };
    }
}
