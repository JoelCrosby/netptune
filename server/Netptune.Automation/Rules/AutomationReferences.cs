using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Models.Automations;
using Netptune.Core.Models.Usage;
using Netptune.Core.Services.Automations;
using Netptune.Core.ViewModels.Automations;

namespace Netptune.Automation.Rules;

public sealed record AutomationReferenceSubject
{
    public UsageSubjectKind Kind { get; init; }

    public int Id { get; init; }

    public string Name { get; init; } = null!;
}

public static class AutomationReferences
{
    public static List<UsageReference> Find(
        List<AutomationRule> rules,
        IAutomationActionRegistry actionRegistry,
        AutomationReferenceSubject subject)
    {
        var ruleViews = rules.ConvertAll(rule => rule.ToViewModel(actionRegistry));
        var referencingRules = ruleViews.Where(rule => References(rule, subject)).ToList();

        return referencingRules.ConvertAll(rule => new UsageReference
        {
            Id = rule.Id,
            Name = rule.Name,
        });
    }

    private static bool References(AutomationRuleViewModel rule, AutomationReferenceSubject subject)
    {
        var isReferencedByAction = rule.Actions.Any(action => References(action, subject));
        var isReferencedByTrigger = References(rule.Trigger.ConditionGroup, subject);

        return isReferencedByAction || isReferencedByTrigger;
    }

    private static bool References(AutomationActionViewModel action, AutomationReferenceSubject subject)
    {
        if (subject.Kind == UsageSubjectKind.Status)
        {
            return action.StatusId == subject.Id;
        }

        if (subject.Kind == UsageSubjectKind.Tag)
        {
            var isAdded = action.AddTags.Any(tag => MatchesName(tag, subject.Name));
            var isRemoved = action.RemoveTags.Any(tag => MatchesName(tag, subject.Name));

            return isAdded || isRemoved;
        }

        var isRelationSubject = action.RelationTypeId == subject.Id;
        var isLinkRelationSubject = action.LinkRelationTypeId == subject.Id;

        return isRelationSubject || isLinkRelationSubject;
    }

    private static bool References(AutomationConditionGroup? group, AutomationReferenceSubject subject)
    {
        if (group is null)
        {
            return false;
        }

        var isReferencedByCondition = group.Conditions.Any(condition => References(condition, subject));
        var isReferencedByNestedGroup = group.Groups.Any(nested => References(nested, subject));

        return isReferencedByCondition || isReferencedByNestedGroup;
    }

    private static bool References(AutomationFieldCondition condition, AutomationReferenceSubject subject)
    {
        if (subject.Kind == UsageSubjectKind.Status)
        {
            var isStatusCondition = condition.Field == TaskChangeField.Status;
            var matchesStatusId = MatchesName(condition.Value, subject.Id.ToString());

            return isStatusCondition && matchesStatusId;
        }

        if (subject.Kind == UsageSubjectKind.Tag)
        {
            var isTagCondition = condition.Field == TaskChangeField.Tags;
            var matchesTagName = MatchesName(condition.Value, subject.Name);

            return isTagCondition && matchesTagName;
        }

        return false;
    }

    private static bool MatchesName(string? value, string expected)
    {
        return string.Equals(value?.Trim(), expected, StringComparison.OrdinalIgnoreCase);
    }
}
