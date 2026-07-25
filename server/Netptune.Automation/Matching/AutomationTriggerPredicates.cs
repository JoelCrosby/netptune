using Netptune.Core.Encoding;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Models.Automations;

namespace Netptune.Automation.Matching;

internal static class AutomationTriggerPredicates
{
    public static AutomationTriggerEvaluation Evaluate(AutomationRule rule, ProjectTask task, DateTime now)
    {
        return rule.TriggerType switch
        {
            AutomationTriggerType.TaskUnassignedFor => Evaluation(MatchesUnassigned(rule, task, now)),
            AutomationTriggerType.TaskDueDateApproaching => Evaluation(MatchesDueDateApproaching(rule, task, now)),
            AutomationTriggerType.TaskOverdue => Evaluation(MatchesOverdue(rule, task, now)),
            AutomationTriggerType.TaskHasNoDueDate => Evaluation(MatchesNoDueDate(task)),
            AutomationTriggerType.TaskInactiveFor => Evaluation(MatchesInactive(rule, task, now)),

            _ => AutomationTriggerEvaluation.NotEvaluable,
        };
    }

    public static bool MatchesUnassigned(AutomationRule rule, ProjectTask task, DateTime now)
    {
        var durationDays = ReadDurationDays(rule, 1);

        if (durationDays is null)
        {
            return false;
        }

        var isUnassigned = task.ProjectTaskAppUsers.Count == 0;
        var hasReachedDuration = LastActivityAt(task) <= now.AddDays(-durationDays.Value);

        return IsCandidate(task) && isUnassigned && hasReachedDuration;
    }

    public static bool MatchesDueDateApproaching(AutomationRule rule, ProjectTask task, DateTime now)
    {
        var durationDays = ReadDurationDays(rule, 0);

        if (durationDays is null)
        {
            return false;
        }

        var localToday = AutomationTimeZones.Today(rule, now);
        var isDueOnLeadDate = task.DueDate == localToday.AddDays(durationDays.Value);

        return IsCandidate(task) && IsOpen(task) && isDueOnLeadDate;
    }

    public static bool MatchesOverdue(AutomationRule rule, ProjectTask task, DateTime now)
    {
        var localToday = AutomationTimeZones.Today(rule, now);
        var isOverdue = task.DueDate < localToday;

        return IsCandidate(task) && IsOpen(task) && isOverdue;
    }

    public static bool MatchesNoDueDate(ProjectTask task)
    {
        var hasNoDueDate = task.DueDate is null;

        return IsCandidate(task) && IsOpen(task) && hasNoDueDate;
    }

    public static bool MatchesInactive(AutomationRule rule, ProjectTask task, DateTime now)
    {
        var durationDays = ReadDurationDays(rule, 1);

        if (durationDays is null)
        {
            return false;
        }

        var hasReachedDuration = LastActivityAt(task) <= now.AddDays(-durationDays.Value);

        return IsCandidate(task) && hasReachedDuration;
    }

    private static AutomationTriggerEvaluation Evaluation(bool isMatch)
    {
        return AutomationTriggerEvaluation.From(isMatch);
    }

    private static bool IsCandidate(ProjectTask task)
    {
        return !task.IsDeleted;
    }

    private static bool IsOpen(ProjectTask task)
    {
        return task.Status?.Category != StatusCategory.Done;
    }

    private static DateTime LastActivityAt(ProjectTask task)
    {
        return task.UpdatedAt ?? task.CreatedAt;
    }

    private static int? ReadDurationDays(AutomationRule rule, int minimum)
    {
        var durationDays = JsonUtils.ReadInt(rule.TriggerConfig, "durationDays");
        var hasValidDuration = durationDays >= minimum && durationDays <= 365;

        return hasValidDuration ? durationDays : null;
    }
}
