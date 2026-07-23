using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Events.Tasks;
using Netptune.Core.Extensions;

namespace Netptune.Core.Models.Automations;

public sealed record AutomationFieldCondition
{
    public TaskChangeField Field { get; init; }

    public AutomationConditionOperator Operator { get; init; }

    public string? Value { get; init; }

    public bool Matches(ProjectTask task, TaskChangedMessage message)
    {
        var change = message.Changes.FirstOrDefault(candidate => candidate.Field == Field);

        if (Operator == AutomationConditionOperator.Any)
        {
            var fieldChanged = change is not null;

            return fieldChanged;
        }

        if (Operator == AutomationConditionOperator.Added)
        {
            var addedValues = change?.AddedValues ?? [];
            var matchingValueWasAdded = MatchesCollection(addedValues);

            return matchingValueWasAdded;
        }

        if (Operator == AutomationConditionOperator.Removed)
        {
            var removedValues = change?.RemovedValues ?? [];
            var matchingValueWasRemoved = MatchesCollection(removedValues);

            return matchingValueWasRemoved;
        }

        var currentValues = CurrentValues(task);
        var hasEqualValue = currentValues.Any(value => value.EqualsOrdinalIgnoreCase(Value));
        var hasNoEqualValues = currentValues.All(value => !value.EqualsOrdinalIgnoreCase(Value));
        var hasContainingValue = currentValues.Any(value => value.ContainsOrdinalIgnoreCase(Value));
        var hasNoValues = currentValues.Count == 0;
        var hasOnlyEmptyValues = currentValues.All(string.IsNullOrWhiteSpace);
        var isEmpty = hasNoValues || hasOnlyEmptyValues;
        var hasNonEmptyValue = currentValues.Any(value => !string.IsNullOrWhiteSpace(value));

        return Operator switch
        {
            AutomationConditionOperator.Equals => hasEqualValue,
            AutomationConditionOperator.NotEquals => hasNoEqualValues,
            AutomationConditionOperator.Contains => hasContainingValue,
            AutomationConditionOperator.IsEmpty => isEmpty,
            AutomationConditionOperator.IsNotEmpty => hasNonEmptyValue,
            _ => false,
        };
    }

    public bool Matches(TaskFieldChange change)
    {
        var newValueEqualsCondition = change.NewValue.EqualsOrdinalIgnoreCase(Value);
        var newValueContainsCondition = change.NewValue.ContainsOrdinalIgnoreCase(Value);
        var newValueIsEmpty = string.IsNullOrWhiteSpace(change.NewValue);
        var matchingValueWasAdded = MatchesCollection(change.AddedValues);
        var matchingValueWasRemoved = MatchesCollection(change.RemovedValues);

        return Operator switch
        {
            AutomationConditionOperator.Any => true,
            AutomationConditionOperator.Equals => newValueEqualsCondition,
            AutomationConditionOperator.NotEquals => !newValueEqualsCondition,
            AutomationConditionOperator.Contains => newValueContainsCondition,
            AutomationConditionOperator.IsEmpty => newValueIsEmpty,
            AutomationConditionOperator.IsNotEmpty => !newValueIsEmpty,
            AutomationConditionOperator.Added => matchingValueWasAdded,
            AutomationConditionOperator.Removed => matchingValueWasRemoved,
            _ => false,
        };
    }

    public string? Validate()
    {
        var isDefinedField = Enum.IsDefined(Field);
        var isDefinedOperator = Enum.IsDefined(Operator);
        var hasSupportedIdentifiers = isDefinedField && isDefinedOperator;

        if (!hasSupportedIdentifiers)
        {
            return "Condition field or operator is not supported.";
        }

        var isCollection = Field is
            TaskChangeField.Assignees or
            TaskChangeField.Tags;

        var isText = Field is
            TaskChangeField.Name or
            TaskChangeField.Description;

        var isChangeCollectionOperator = Operator is
            AutomationConditionOperator.Added or
            AutomationConditionOperator.Removed;

        var isScalarOperator = Operator is
            AutomationConditionOperator.Equals or
            AutomationConditionOperator.NotEquals or
            AutomationConditionOperator.IsEmpty or
            AutomationConditionOperator.IsNotEmpty;

        var isCollectionStateOperator = Operator is
            AutomationConditionOperator.Equals or
            AutomationConditionOperator.NotEquals or
            AutomationConditionOperator.Contains or
            AutomationConditionOperator.IsEmpty or
            AutomationConditionOperator.IsNotEmpty;

        var isAnyChangeOperator = Operator == AutomationConditionOperator.Any;
        var isSupportedCollectionOperator = isChangeCollectionOperator || isCollectionStateOperator;
        var isCollectionSupported = isCollection && isSupportedCollectionOperator;
        var isTextContainsOperator = isText && Operator == AutomationConditionOperator.Contains;
        var isSupportedScalarOperator = isScalarOperator || isTextContainsOperator;
        var isScalarSupported = !isCollection && isSupportedScalarOperator;
        var isSupported = isAnyChangeOperator || isCollectionSupported || isScalarSupported;

        if (!isSupported)
        {
            return $"Operator '{Operator}' is not supported for field '{Field}'.";
        }

        var requiresValue = Operator is
            AutomationConditionOperator.Equals or
            AutomationConditionOperator.NotEquals or
            AutomationConditionOperator.Contains;
        var hasValue = !string.IsNullOrWhiteSpace(Value);
        var isRequiredValueMissing = requiresValue && !hasValue;

        if (isRequiredValueMissing)
        {
            return $"Condition operator '{Operator}' requires a value.";
        }

        return ValidateValue();
    }

    private string? ValidateValue()
    {
        if (string.IsNullOrWhiteSpace(Value))
        {
            return null;
        }

        var hasValidValue = Field switch
        {
            TaskChangeField.Status => int.TryParse(Value, out _),
            TaskChangeField.Priority => Enum.TryParse<TaskPriority>(Value, true, out _),
            TaskChangeField.Estimate => Enum.TryParse<EstimateType>(Value, true, out _),
            TaskChangeField.StartDate or TaskChangeField.DueDate => DateOnly.TryParse(Value, out _),
            _ => true,
        };

        if (!hasValidValue)
        {
            return $"Condition value '{Value}' is invalid for field '{Field}'.";
        }

        return null;
    }

    private List<string> CurrentValues(ProjectTask task)
    {
        var scalarValue = Field switch
        {
            TaskChangeField.Name => task.Name,
            TaskChangeField.Description => task.Description,
            TaskChangeField.Status => task.StatusId.ToString(),
            TaskChangeField.Owner => task.OwnerId,
            TaskChangeField.Priority => task.Priority?.ToString(),
            TaskChangeField.Estimate => task.EstimateType?.ToString(),
            TaskChangeField.DueDate => task.DueDate?.ToString("yyyy-MM-dd"),
            TaskChangeField.StartDate => task.StartDate?.ToString("yyyy-MM-dd"),
            _ => null,
        };

        if (scalarValue is not null)
        {
            return [scalarValue];
        }

        if (Field == TaskChangeField.Assignees)
        {
            var assigneeUserIds = task.ProjectTaskAppUsers
                .Select(assignee => assignee.UserId)
                .ToList();

            return assigneeUserIds;
        }

        if (Field == TaskChangeField.Tags)
        {
            var tagNames = task.Tags.Select(tag => tag.Name).ToList();

            return tagNames;
        }

        return [];
    }

    private bool MatchesCollection(List<string> values)
    {
        var hasExpectedValue = !string.IsNullOrWhiteSpace(Value);

        if (!hasExpectedValue)
        {
            var collectionChanged = values.Count > 0;

            return collectionChanged;
        }

        var containsExpectedValue = values.Any(value => value.EqualsOrdinalIgnoreCase(Value));

        return containsExpectedValue;
    }
}
