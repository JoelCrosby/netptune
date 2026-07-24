using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Events.Tasks;

namespace Netptune.Core.Models.Automations;

public sealed record AutomationConditionGroup
{
    private const int MaximumDepth = 4;
    private const int MaximumConditionCount = 50;

    public AutomationConditionGroupOperator Operator { get; init; }

    public List<AutomationFieldCondition> Conditions { get; init; } = [];

    public List<AutomationConditionGroup> Groups { get; init; } = [];

    public bool Matches(ProjectTask task, TaskChangedMessage message)
    {
        return Matches(task, message, true);
    }

    public bool Matches(ProjectTask task)
    {
        return Matches(task, null, false);
    }

    private bool Matches(ProjectTask task, TaskChangedMessage? message, bool supportsChangeOperators)
    {
        var conditionResults = Conditions
            .Select(condition => condition.Matches(task, message, supportsChangeOperators))
            .ToList();

        var nestedGroupResults = Groups
            .Select(group => group.Matches(task, message, supportsChangeOperators))
            .ToList();

        var memberResults = conditionResults.Concat(nestedGroupResults).ToList();

        return ResolveMatch(memberResults);
    }

    public AutomationConditionGroupExplanation Explain(
        ProjectTask task,
        TaskChangedMessage? message,
        bool supportsChangeOperators)
    {
        var conditions = Conditions
            .Select(condition => condition.Explain(task, message, supportsChangeOperators))
            .ToList();

        var groups = Groups
            .Select(group => group.Explain(task, message, supportsChangeOperators))
            .ToList();

        var conditionResults = conditions.Select(condition => condition.IsMatch);
        var nestedGroupResults = groups.Select(group => group.IsMatch);
        var memberResults = conditionResults.Concat(nestedGroupResults).ToList();

        return new AutomationConditionGroupExplanation
        {
            Operator = Operator,
            IsMatch = ResolveMatch(memberResults),
            Conditions = conditions,
            Groups = groups,
        };
    }

    private bool ResolveMatch(List<bool> memberResults)
    {
        if (memberResults.Count == 0)
        {
            return false;
        }

        var allMembersMatch = memberResults.All(result => result);
        var anyMemberMatches = memberResults.Any(result => result);
        var noMembersMatch = memberResults.All(result => !result);

        return Operator switch
        {
            AutomationConditionGroupOperator.All => allMembersMatch,
            AutomationConditionGroupOperator.Any => anyMemberMatches,
            AutomationConditionGroupOperator.None => noMembersMatch,
            _ => false,
        };
    }

    public string? Validate(bool supportsChangeOperators = true)
    {
        var result = Validate(1, supportsChangeOperators);

        return result.Error;
    }

    private ConditionGroupValidationResult Validate(int depth, bool supportsChangeOperators)
    {
        if (!Enum.IsDefined(Operator))
        {
            var error = $"Condition group operator '{Operator}' is not supported.";

            return new ConditionGroupValidationResult(error, 0);
        }

        if (depth > MaximumDepth)
        {
            var error = $"Condition groups cannot be nested more than {MaximumDepth} levels.";

            return new ConditionGroupValidationResult(error, 0);
        }

        var conditionCount = Conditions.Count;

        if (conditionCount > MaximumConditionCount)
        {
            var error = $"Automations cannot have more than {MaximumConditionCount} field conditions.";

            return new ConditionGroupValidationResult(error, conditionCount);
        }

        var hasNoConditions = Conditions.Count == 0;
        var hasNoNestedGroups = Groups.Count == 0;
        var isEmpty = hasNoConditions && hasNoNestedGroups;

        if (isEmpty)
        {
            const string error = "Condition groups require at least one condition or nested group.";

            return new ConditionGroupValidationResult(error, conditionCount);
        }

        foreach (var condition in Conditions)
        {
            var error = condition.Validate(supportsChangeOperators);

            if (error is not null)
            {
                return new ConditionGroupValidationResult(error, conditionCount);
            }
        }

        foreach (var nestedGroup in Groups)
        {
            var result = nestedGroup.Validate(depth + 1, supportsChangeOperators);

            if (result.Error is not null)
            {
                return result;
            }

            conditionCount += result.ConditionCount;

            if (conditionCount > MaximumConditionCount)
            {
                var error = $"Automations cannot have more than {MaximumConditionCount} field conditions.";

                return new ConditionGroupValidationResult(error, conditionCount);
            }
        }

        return new ConditionGroupValidationResult(null, conditionCount);
    }

    private sealed record ConditionGroupValidationResult(string? Error, int ConditionCount);
}
