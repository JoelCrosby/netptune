using FluentAssertions;

using Netptune.Core.Constants;
using Netptune.Core.Enums;
using Netptune.Query.Model;
using Netptune.Query.Tasks;
using Netptune.Query.Validation;

using Xunit;

namespace Netptune.UnitTests.Netptune.Query.Validation;

public class QueryValidatorTests
{
    [Fact]
    public void Validate_WithNoQuery_IsValid()
    {
        QueryValidator.Validate(TaskFieldCatalog.Instance, null).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyGroup_IsValid()
    {
        QueryValidator.Validate(TaskFieldCatalog.Instance, new QueryGroup()).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_UnknownField_IsRejected()
    {
        var result = Validate("task.colour", QueryOperator.Equals, "blue");

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Field.Should().Be("task.colour");
        result.Errors[0].Message.Should().Contain("not a known task field");
    }

    [Fact]
    public void Validate_UndefinedOperator_IsRejected()
    {
        var result = Validate(TaskFieldKeys.Name, (QueryOperator)99, "x");

        result.Errors.Should().ContainSingle();
        result.Errors[0].Message.Should().Contain("not supported");
    }

    [Fact]
    public void Validate_OperatorNotPermittedForField_IsRejected()
    {
        var result = Validate(TaskFieldKeys.Status, QueryOperator.Contains, "done");

        result.Errors.Should().ContainSingle();
        result.Errors[0].Message.Should().Contain("cannot be used with 'Status'");
    }

    [Fact]
    public void Validate_IsOverdueOnAFieldThatCannotBeOverdue_IsRejected()
    {
        var result = Validate(TaskFieldKeys.StartDate, QueryOperator.IsOverdue);

        result.Errors.Should().ContainSingle();
        result.Errors[0].Message.Should().Contain("cannot be used with 'Start date'");
    }

    [Theory]
    [InlineData(QueryOperator.Between, 1)]
    [InlineData(QueryOperator.Between, 3)]
    [InlineData(QueryOperator.Equals, 0)]
    [InlineData(QueryOperator.Equals, 2)]
    public void Validate_WrongValueCount_IsRejected(QueryOperator queryOperator, int valueCount)
    {
        var values = Enumerable.Repeat("2026-08-21", valueCount).ToArray();
        var result = Validate(TaskFieldKeys.DueDate, queryOperator, values);

        result.Errors.Should().ContainSingle();
        result.Errors[0].Message.Should().Contain("requires");
    }

    [Fact]
    public void Validate_EmptinessOperatorWithValues_IsRejected()
    {
        var result = Validate(TaskFieldKeys.Tags, QueryOperator.IsEmpty, "regression");

        result.Errors.Should().ContainSingle();
        result.Errors[0].Message.Should().Contain("requires no values");
    }

    [Fact]
    public void Validate_SetMembershipWithNoValues_IsRejected()
    {
        var result = Validate(TaskFieldKeys.Status, QueryOperator.In);

        result.Errors.Should().ContainSingle();
        result.Errors[0].Message.Should().Contain("at least one value");
    }

    [Fact]
    public void Validate_UnparsableDate_IsRejected()
    {
        var result = Validate(TaskFieldKeys.DueDate, QueryOperator.Equals, "next tuesday");

        result.Errors.Should().ContainSingle();
        result.Errors[0].Message.Should().Contain("not a valid date value");
    }

    [Fact]
    public void Validate_UnparsableNumber_IsRejected()
    {
        var result = Validate(TaskFieldKeys.EstimateValue, QueryOperator.Equals, "a few");

        result.Errors.Should().ContainSingle();
    }

    [Fact]
    public void Validate_EnumValueOutsideTheEnum_IsRejected()
    {
        var result = Validate(TaskFieldKeys.Priority, QueryOperator.Equals, "17");

        result.Errors.Should().ContainSingle();
    }

    [Fact]
    public void Validate_EnumValueByName_IsAccepted()
    {
        Validate(TaskFieldKeys.Priority, QueryOperator.Equals, "critical").IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_NegativeDayCount_IsRejected()
    {
        var result = Validate(TaskFieldKeys.DueDate, QueryOperator.InNextDays, "-3");

        result.Errors.Should().ContainSingle();
        result.Errors[0].Message.Should().Contain("not a valid number of days");
    }

    [Fact]
    public void Validate_MalformedRelationReference_IsRejected()
    {
        var result = Validate(TaskFieldKeys.Relations, QueryOperator.In, "3:sideways");

        result.Errors.Should().ContainSingle();
    }

    [Fact]
    public void Validate_TooDeeplyNested_IsRejected()
    {
        var group = BuildNested(5);
        var result = QueryValidator.Validate(TaskFieldCatalog.Instance, group);

        result.Errors.Should().ContainSingle();
        result.Errors[0].Message.Should().Contain("nested more than 4 levels");
    }

    [Fact]
    public void Validate_AtTheDepthLimit_IsAccepted()
    {
        QueryValidator.Validate(TaskFieldCatalog.Instance, BuildNested(4)).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_TooManyConditions_IsRejected()
    {
        var conditions = Enumerable
            .Range(0, ConditionGroupLimits.MaximumConditionCount + 1)
            .Select(_ => new QueryCondition
            {
                Field = TaskFieldKeys.Status,
                Operator = QueryOperator.Equals,
                Values = ["1"],
            })
            .ToList();
        var result = QueryValidator.Validate(TaskFieldCatalog.Instance, new QueryGroup { Conditions = conditions });

        result.Errors.Should().ContainSingle();
        result.Errors[0].Message.Should().Contain("more than 50 conditions");
    }

    [Fact]
    public void Validate_CountsConditionsAcrossNestedGroups()
    {
        var group = new QueryGroup
        {
            Conditions = BuildConditions(30),
            Groups = [new QueryGroup { Conditions = BuildConditions(21) }],
        };
        var result = QueryValidator.Validate(TaskFieldCatalog.Instance, group);

        result.Errors.Should().ContainSingle();
        result.Errors[0].Message.Should().Contain("more than 50 conditions");
    }

    [Fact]
    public void Validate_ReportsEveryErrorRatherThanTheFirst()
    {
        var group = new QueryGroup
        {
            Conditions =
            [
                new QueryCondition { Field = "task.colour", Operator = QueryOperator.Equals, Values = ["blue"] },
                new QueryCondition { Field = TaskFieldKeys.DueDate, Operator = QueryOperator.Equals, Values = ["soon"] },
            ],
            Groups =
            [
                new QueryGroup
                {
                    Conditions = [new QueryCondition { Field = TaskFieldKeys.Status, Operator = QueryOperator.In }],
                },
            ],
        };
        var result = QueryValidator.Validate(TaskFieldCatalog.Instance, group);

        result.Errors.Should().HaveCount(3);
        result.Errors.Select(error => error.Path).Should().Equal(
            "query.conditions[0]",
            "query.conditions[1]",
            "query.groups[0].conditions[0]");
    }

    [Fact]
    public void Validate_UndefinedGroupOperator_IsRejected()
    {
        var group = new QueryGroup
        {
            Operator = (QueryGroupOperator)7,
            Conditions = BuildConditions(1),
        };
        var result = QueryValidator.Validate(TaskFieldCatalog.Instance, group);

        result.Errors.Should().ContainSingle();
        result.Errors[0].Message.Should().Contain("Group operator");
    }

    private static List<QueryCondition> BuildConditions(int count)
    {
        return Enumerable
            .Range(0, count)
            .Select(_ => new QueryCondition
            {
                Field = TaskFieldKeys.Status,
                Operator = QueryOperator.Equals,
                Values = ["1"],
            })
            .ToList();
    }

    private static QueryGroup BuildNested(int depth)
    {
        var group = new QueryGroup { Conditions = BuildConditions(1) };

        for (var level = 1; level < depth; level++)
        {
            group = new QueryGroup { Groups = [group] };
        }

        return group;
    }

    private static QueryValidationResult Validate(string fieldKey, QueryOperator queryOperator, params string[] values)
    {
        var group = new QueryGroup
        {
            Conditions =
            [
                new QueryCondition
                {
                    Field = fieldKey,
                    Operator = queryOperator,
                    Values = [.. values],
                },
            ],
        };

        return QueryValidator.Validate(TaskFieldCatalog.Instance, group);
    }
}
