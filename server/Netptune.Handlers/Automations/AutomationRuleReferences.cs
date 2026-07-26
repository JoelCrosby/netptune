using Netptune.Core.Enums;
using Netptune.Core.Models.Automations;
using Netptune.Core.ViewModels.Automations;

namespace Netptune.Handlers.Automations;

internal sealed record AutomationRuleReferences
{
    public HashSet<int> ProjectIds { get; init; } = [];

    public HashSet<int> BoardIds { get; init; } = [];

    public HashSet<int> SprintIds { get; init; } = [];

    public HashSet<int> StatusIds { get; init; } = [];

    public HashSet<int> BoardGroupIds { get; init; } = [];

    public HashSet<int> RelationTypeIds { get; init; } = [];

    public HashSet<int> TaskIds { get; init; } = [];

    public HashSet<string> TagNames { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    public HashSet<string> UserIds { get; init; } = new(StringComparer.Ordinal);

    public void Add(AutomationRuleViewModel rule)
    {
        AddId(ProjectIds, rule.ProjectId);
        AddId(BoardIds, rule.BoardId);
        AddId(SprintIds, rule.SprintId);
        AddName(UserIds, rule.ExecutionUserId);

        AddConditions(rule.Trigger.ConditionGroup);

        foreach (var action in rule.Actions)
        {
            AddAction(action);
        }
    }

    private void AddAction(AutomationActionViewModel action)
    {
        AddId(StatusIds, action.StatusId);
        AddId(SprintIds, action.SprintId);
        AddId(BoardGroupIds, action.BoardGroupId);
        AddId(RelationTypeIds, action.RelationTypeId);
        AddId(RelationTypeIds, action.LinkRelationTypeId);
        AddId(TaskIds, action.RelatedTaskId);
        AddName(UserIds, action.OwnerId);

        foreach (var assigneeId in action.AssigneeIds ?? [])
        {
            AddName(UserIds, assigneeId);
        }

        foreach (var recipientId in action.RecipientUserIds)
        {
            AddName(UserIds, recipientId);
        }

        foreach (var tag in action.AddTags.Concat(action.RemoveTags))
        {
            AddName(TagNames, tag);
        }
    }

    private void AddConditions(AutomationConditionGroup? group)
    {
        if (group is null)
        {
            return;
        }

        foreach (var condition in group.Conditions)
        {
            AddCondition(condition);
        }

        foreach (var nestedGroup in group.Groups)
        {
            AddConditions(nestedGroup);
        }
    }

    private void AddCondition(AutomationFieldCondition condition)
    {
        var value = condition.Value;

        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        switch (condition.Field)
        {
            case TaskChangeField.Status:
                AddParsedId(StatusIds, value);
                break;
            case TaskChangeField.Sprint:
                AddParsedId(SprintIds, value);
                break;
            case TaskChangeField.Tags:
                AddName(TagNames, value);
                break;
            case TaskChangeField.Owner:
            case TaskChangeField.Assignees:
                AddName(UserIds, value);
                break;
        }
    }

    private static void AddId(HashSet<int> target, int? id)
    {
        if (!id.HasValue)
        {
            return;
        }

        target.Add(id.Value);
    }

    private static void AddParsedId(HashSet<int> target, string value)
    {
        var isNumeric = int.TryParse(value, out var id);

        if (!isNumeric)
        {
            return;
        }

        target.Add(id);
    }

    private static void AddName(HashSet<string> target, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        target.Add(value);
    }
}
