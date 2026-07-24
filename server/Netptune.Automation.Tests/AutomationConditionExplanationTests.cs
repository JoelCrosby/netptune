using FluentAssertions;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Models.Automations;

using Xunit;

namespace Netptune.Automation.Tests;

public sealed class AutomationConditionExplanationTests
{
    [Fact]
    public void Explain_reports_matching_condition_with_actual_value()
    {
        var task = CreateTask();
        var group = CreateGroup(AutomationConditionGroupOperator.All, new AutomationFieldCondition
        {
            Field = TaskChangeField.Priority,
            Operator = AutomationConditionOperator.Equals,
            Value = nameof(TaskPriority.High),
        });

        var explanation = group.Explain(task, null, false);

        explanation.IsMatch.Should().BeTrue();
        explanation.Conditions.Should().ContainSingle();
        explanation.Conditions[0].IsMatch.Should().BeTrue();
        explanation.Conditions[0].IsEvaluable.Should().BeTrue();
        explanation.Conditions[0].ActualValue.Should().Be(nameof(TaskPriority.High));
    }

    [Fact]
    public void Explain_reports_failing_condition_with_actual_value()
    {
        var task = CreateTask();
        var group = CreateGroup(AutomationConditionGroupOperator.All, new AutomationFieldCondition
        {
            Field = TaskChangeField.Name,
            Operator = AutomationConditionOperator.Equals,
            Value = "Something else",
        });

        var explanation = group.Explain(task, null, false);

        explanation.IsMatch.Should().BeFalse();
        explanation.Conditions[0].IsMatch.Should().BeFalse();
        explanation.Conditions[0].ActualValue.Should().Be("Fix login redirect");
    }

    [Fact]
    public void Explain_marks_change_operators_unevaluable_without_a_change()
    {
        var task = CreateTask();
        var group = CreateGroup(AutomationConditionGroupOperator.All, new AutomationFieldCondition
        {
            Field = TaskChangeField.Status,
            Operator = AutomationConditionOperator.Any,
        });

        var explanation = group.Explain(task, null, true);

        explanation.Conditions[0].IsEvaluable.Should().BeFalse();
        explanation.Conditions[0].IsMatch.Should().BeFalse();
    }

    [Fact]
    public void Explain_joins_collection_values()
    {
        var task = CreateTask();
        var group = CreateGroup(AutomationConditionGroupOperator.All, new AutomationFieldCondition
        {
            Field = TaskChangeField.Tags,
            Operator = AutomationConditionOperator.Contains,
            Value = "bug",
        });

        var explanation = group.Explain(task, null, false);

        explanation.Conditions[0].IsMatch.Should().BeTrue();
        explanation.Conditions[0].ActualValue.Should().Be("bug, urgent");
    }

    [Fact]
    public void Explain_group_match_follows_the_group_operator()
    {
        var task = CreateTask();
        var matching = new AutomationFieldCondition
        {
            Field = TaskChangeField.Priority,
            Operator = AutomationConditionOperator.Equals,
            Value = nameof(TaskPriority.High),
        };
        var failing = new AutomationFieldCondition
        {
            Field = TaskChangeField.Name,
            Operator = AutomationConditionOperator.Equals,
            Value = "Something else",
        };

        var allGroup = CreateGroup(AutomationConditionGroupOperator.All, matching, failing);
        var anyGroup = CreateGroup(AutomationConditionGroupOperator.Any, matching, failing);
        var noneGroup = CreateGroup(AutomationConditionGroupOperator.None, failing);

        allGroup.Explain(task, null, false).IsMatch.Should().BeFalse();
        anyGroup.Explain(task, null, false).IsMatch.Should().BeTrue();
        noneGroup.Explain(task, null, false).IsMatch.Should().BeTrue();
    }

    [Fact]
    public void Explain_includes_nested_groups()
    {
        var task = CreateTask();
        var nested = CreateGroup(AutomationConditionGroupOperator.All, new AutomationFieldCondition
        {
            Field = TaskChangeField.Priority,
            Operator = AutomationConditionOperator.Equals,
            Value = nameof(TaskPriority.High),
        });

        var group = new AutomationConditionGroup
        {
            Operator = AutomationConditionGroupOperator.All,
            Groups = [nested],
        };

        var explanation = group.Explain(task, null, false);

        explanation.IsMatch.Should().BeTrue();
        explanation.Groups.Should().ContainSingle();
        explanation.Groups[0].Conditions[0].IsMatch.Should().BeTrue();
    }

    [Fact]
    public void Explain_agrees_with_matches()
    {
        var task = CreateTask();
        var group = CreateGroup(AutomationConditionGroupOperator.All, new AutomationFieldCondition
        {
            Field = TaskChangeField.Priority,
            Operator = AutomationConditionOperator.Equals,
            Value = nameof(TaskPriority.Low),
        });

        var explanation = group.Explain(task, null, false);

        explanation.IsMatch.Should().Be(group.Matches(task));
    }

    private static AutomationConditionGroup CreateGroup(
        AutomationConditionGroupOperator groupOperator,
        params AutomationFieldCondition[] conditions)
    {
        return new AutomationConditionGroup
        {
            Operator = groupOperator,
            Conditions = [.. conditions],
        };
    }

    private static ProjectTask CreateTask()
    {
        return new ProjectTask
        {
            Id = 1,
            Name = "Fix login redirect",
            Priority = TaskPriority.High,
            Tags = [new Tag { Name = "bug" }, new Tag { Name = "urgent" }],
        };
    }
}
