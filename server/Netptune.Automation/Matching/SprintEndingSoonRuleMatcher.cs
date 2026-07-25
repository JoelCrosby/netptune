using System.Diagnostics;

using Microsoft.Extensions.Logging;

using Netptune.Automation.Common;
using Netptune.Automation.Diagnostics;
using Netptune.Automation.Models;
using Netptune.Core.Encoding;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.UnitOfWork;

namespace Netptune.Automation.Matching;

internal sealed class SprintEndingSoonRuleMatcher : IScheduledRuleMatcher
{
    private const int MaximumDurationDays = 365;

    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly ILogger<SprintEndingSoonRuleMatcher> Logger;

    public AutomationTriggerType TriggerType => AutomationTriggerType.SprintEndingSoon;

    public SprintEndingSoonRuleMatcher(INetptuneUnitOfWork unitOfWork, ILogger<SprintEndingSoonRuleMatcher> logger)
    {
        UnitOfWork = unitOfWork;
        Logger = logger;
    }

    public async Task<List<PendingAutomationExecution>> Match(CancellationToken cancellationToken)
    {
        var activity = Activity.Current;

        var rules = await UnitOfWork.Automations.GetEnabledRulesForTrigger(
            TriggerType,
            cancellationToken: cancellationToken);

        Telemetry.RecordRulesEvaluated(TriggerType, rules.Count);
        activity?.SetTag("automation.rules.evaluated", rules.Count);

        var ruleDefinitions = rules
            .Select(rule => new
            {
                Rule = rule,
                DurationDays = JsonUtils.ReadInt(rule.TriggerConfig, "durationDays"),
            })
            .Where(definition => definition.DurationDays is >= 0 and <= MaximumDurationDays)
            .Select(definition => new SprintEndingSoonRuleDefinition(definition.Rule, definition.DurationDays!.Value))
            .ToList();

        var invalidRuleCount = rules.Count - ruleDefinitions.Count;

        if (invalidRuleCount > 0)
        {
            Logger.LogWarning(
                "Skipped {InvalidRuleCount} sprint-ending automation rules with missing or invalid durationDays",
                invalidRuleCount);
            Telemetry.RecordRulesSkipped(TriggerType, invalidRuleCount, "invalid_config");
        }

        if (ruleDefinitions.Count == 0)
        {
            return [];
        }

        var now = DateTime.UtcNow;
        var workspaceIds = ruleDefinitions.Select(definition => definition.Rule.WorkspaceId).Distinct().ToList();
        var furthestDuration = ruleDefinitions.Max(definition => definition.DurationDays);
        var latestEndDate = now.Date.AddDays(furthestDuration + 1);
        var sprints = await UnitOfWork.Sprints.GetActiveSprintsEndingBefore(
            workspaceIds,
            latestEndDate,
            cancellationToken);

        activity?.SetTag("automation.sprints.candidate", sprints.Count);

        if (sprints.Count == 0)
        {
            return [];
        }

        var dueSprintsByRule = ruleDefinitions
            .Select(definition => new SprintEndingSoonCandidate(
                definition,
                sprints
                    .Where(sprint => sprint.WorkspaceId == definition.Rule.WorkspaceId)
                    .Where(sprint => IsEndingSoon(definition, sprint, now))
                    .ToList()))
            .Where(candidate => candidate.Sprints.Count > 0)
            .ToList();

        var dueSprintIds = dueSprintsByRule
            .SelectMany(candidate => candidate.Sprints.Select(sprint => sprint.Id))
            .Distinct()
            .ToList();

        var tasks = await UnitOfWork.Tasks.GetSprintAutomationTasks(dueSprintIds, cancellationToken);
        var tasksBySprint = tasks
            .Where(task => task.SprintId.HasValue)
            .GroupBy(task => task.SprintId!.Value)
            .ToDictionary(group => group.Key, group => group.ToList());

        activity?.SetTag("automation.tasks.candidate", tasks.Count);

        var executions = new List<PendingAutomationExecution>();

        foreach (var candidate in dueSprintsByRule)
        {
            foreach (var sprint in candidate.Sprints)
            {
                var hasTasks = tasksBySprint.TryGetValue(sprint.Id, out var sprintTasks);

                if (!hasTasks)
                {
                    continue;
                }

                var matchingTasks = sprintTasks!
                    .Where(task => AutomationRuleConditions.Match(candidate.Definition.Rule, task));

                foreach (var task in matchingTasks)
                {
                    executions.Add(new PendingAutomationExecution
                    {
                        Rule = candidate.Definition.Rule,
                        Task = task,
                        ExecutionUserId = candidate.Definition.Rule.ExecutionUserId,
                        IdempotencyKey = BuildIdempotencyKey(candidate.Definition.Rule.Id, task.Id, sprint),
                        TriggeredAt = now,
                    });
                }
            }
        }

        Telemetry.RecordRulesMatched(TriggerType, executions.Count);
        activity?.SetTag("automation.rules.matched", executions.Count);

        return executions;
    }

    private static bool IsEndingSoon(SprintEndingSoonRuleDefinition definition, Sprint sprint, DateTime now)
    {
        var today = AutomationTimeZones.Today(definition.Rule, now);
        var endDate = DateOnly.FromDateTime(sprint.EndDate);
        var daysRemaining = endDate.DayNumber - today.DayNumber;

        return daysRemaining >= 0 && daysRemaining <= definition.DurationDays;
    }

    private static string BuildIdempotencyKey(int ruleId, int taskId, Sprint sprint)
    {
        return $"rule:{ruleId}:task:{taskId}:sprint:{sprint.Id}:ending:{sprint.EndDate:yyyy-MM-dd}";
    }
}
