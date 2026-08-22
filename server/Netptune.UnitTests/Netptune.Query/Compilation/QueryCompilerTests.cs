using Dapper;

using FluentAssertions;

using Netptune.Core.Constants;
using Netptune.Core.Enums;
using Netptune.Query.Compilation;
using Netptune.Query.Model;
using Netptune.Query.Schema;
using Netptune.Query.Tasks;

using Xunit;

namespace Netptune.UnitTests.Netptune.Query.Compilation;

public class QueryCompilerTests
{
    private static readonly QueryCompilationContext Context = new()
    {
        Today = new DateOnly(2026, 8, 21),
        TimeZone = TimeZoneInfo.Utc,
    };

    [Fact]
    public void Compile_WithNoQuery_MatchesEverything()
    {
        var compilation = QueryCompiler.Compile(TaskFieldCatalog.Instance, null, Context);

        compilation.Predicate.Should().Be("TRUE");
        compilation.Parameters.ParameterNames.Should().BeEmpty();
    }

    [Fact]
    public void Compile_WithEmptyGroup_MatchesNothing()
    {
        var compilation = QueryCompiler.Compile(TaskFieldCatalog.Instance, new QueryGroup(), Context);

        compilation.Predicate.Should().Be("FALSE");
    }

    [Fact]
    public void Compile_TextEquals_LowersBothSides()
    {
        var compilation = Compile(TaskFieldKeys.Name, QueryOperator.Equals, "Fix The Export");

        compilation.Predicate.Should().Be("LOWER(pt.name) = @q0");
        Read<string>(compilation, "q0").Should().Be("fix the export");
    }

    [Fact]
    public void Compile_TextContains_EscapesWildcards()
    {
        var compilation = Compile(TaskFieldKeys.Name, QueryOperator.Contains, "50%_off");

        compilation.Predicate.Should().Be(@"LOWER(pt.name) LIKE @q0 ESCAPE '\'");
        Read<string>(compilation, "q0").Should().Be(@"%50\%\_off%");
    }

    [Fact]
    public void Compile_TextStartsWith_AnchorsThePattern()
    {
        var compilation = Compile(TaskFieldKeys.Name, QueryOperator.StartsWith, "spike");

        compilation.Predicate.Should().Be(@"LOWER(pt.name) LIKE @q0 ESCAPE '\'");
        Read<string>(compilation, "q0").Should().Be("spike%");
    }

    [Fact]
    public void Compile_TextIsEmpty_TreatsBlankAsEmpty()
    {
        var compilation = Compile(TaskFieldKeys.Description, QueryOperator.IsEmpty);

        compilation.Predicate.Should().Be("(pt.description IS NULL OR pt.description = '')");
    }

    [Fact]
    public void Compile_StatusIn_UsesAnArrayParameter()
    {
        var compilation = Compile(TaskFieldKeys.Status, QueryOperator.In, "4", "9");

        compilation.Predicate.Should().Be("pt.status_id = ANY(@q0)");
        Read<int[]>(compilation, "q0").Should().Equal(4, 9);
    }

    [Fact]
    public void Compile_StatusNotIn_AllowsNull()
    {
        var compilation = Compile(TaskFieldKeys.Status, QueryOperator.NotIn, "4");

        compilation.Predicate.Should().Be("(pt.status_id IS NULL OR NOT (pt.status_id = ANY(@q0)))");
    }

    [Fact]
    public void Compile_StatusCategory_AcceptsEnumNames()
    {
        var compilation = Compile(TaskFieldKeys.StatusCategory, QueryOperator.Equals, "Done");

        compilation.Predicate.Should().Be("st.category = @q0");
        Read<int>(compilation, "q0").Should().Be((int)StatusCategory.Done);
    }

    [Fact]
    public void Compile_PriorityNone_AlsoMatchesNull()
    {
        var compilation = Compile(TaskFieldKeys.Priority, QueryOperator.Equals, "0");

        compilation.Predicate.Should().Be("(pt.priority = @q0 OR pt.priority IS NULL)");
    }

    [Fact]
    public void Compile_PriorityHigh_DoesNotMatchNull()
    {
        var compilation = Compile(TaskFieldKeys.Priority, QueryOperator.Equals, "High");

        compilation.Predicate.Should().Be("pt.priority = @q0");
        Read<int>(compilation, "q0").Should().Be((int)TaskPriority.High);
    }

    [Fact]
    public void Compile_PriorityInIncludingNone_AlsoMatchesNull()
    {
        var compilation = Compile(TaskFieldKeys.Priority, QueryOperator.In, "0", "3");

        compilation.Predicate.Should().Be("(pt.priority = ANY(@q0) OR pt.priority IS NULL)");
    }

    [Fact]
    public void Compile_EstimateBetween_EmitsAnInclusiveRange()
    {
        var compilation = Compile(TaskFieldKeys.EstimateValue, QueryOperator.Between, "1.5", "8");

        compilation.Predicate.Should().Be("(pt.estimate_value >= @q0 AND pt.estimate_value <= @q1)");
        Read<decimal>(compilation, "q0").Should().Be(1.5m);
        Read<decimal>(compilation, "q1").Should().Be(8m);
    }

    [Fact]
    public void Compile_DueDateInNextDays_SpansTodayThroughTodayPlusN()
    {
        var compilation = Compile(TaskFieldKeys.DueDate, QueryOperator.InNextDays, "7");

        compilation.Predicate.Should().Be("(pt.due_date >= @q0 AND pt.due_date <= @q1)");
        Read<DateOnly>(compilation, "q0").Should().Be(new DateOnly(2026, 8, 21));
        Read<DateOnly>(compilation, "q1").Should().Be(new DateOnly(2026, 8, 28));
    }

    [Fact]
    public void Compile_DueDateInLastDays_SpansTodayMinusNThroughToday()
    {
        var compilation = Compile(TaskFieldKeys.DueDate, QueryOperator.InLastDays, "3");

        Read<DateOnly>(compilation, "q0").Should().Be(new DateOnly(2026, 8, 18));
        Read<DateOnly>(compilation, "q1").Should().Be(new DateOnly(2026, 8, 21));
    }

    [Fact]
    public void Compile_DueDateInNextDays_CrossesTheYearBoundary()
    {
        var context = new QueryCompilationContext
        {
            Today = new DateOnly(2026, 12, 30),
            TimeZone = TimeZoneInfo.Utc,
        };
        var group = GroupFor(TaskFieldKeys.DueDate, QueryOperator.InNextDays, "5");
        var compilation = QueryCompiler.Compile(TaskFieldCatalog.Instance, group, context);

        Read<DateOnly>(compilation, "q1").Should().Be(new DateOnly(2027, 1, 4));
    }

    [Fact]
    public void Compile_IsOverdue_ExcludesDoneTasks()
    {
        var compilation = Compile(TaskFieldKeys.DueDate, QueryOperator.IsOverdue);

        compilation.Predicate.Should().Be("(pt.due_date IS NOT NULL AND pt.due_date < @q0 AND st.category <> @q1)");
        Read<DateOnly>(compilation, "q0").Should().Be(new DateOnly(2026, 8, 21));
        Read<int>(compilation, "q1").Should().Be((int)StatusCategory.Done);
    }

    [Fact]
    public void Compile_CreatedAtOnADay_ResolvesTheWorkspaceDayInUtc()
    {
        var context = new QueryCompilationContext
        {
            Today = new DateOnly(2026, 8, 21),
            TimeZone = TimeZoneInfo.FindSystemTimeZoneById("Australia/Sydney"),
        };
        var group = GroupFor(TaskFieldKeys.CreatedAt, QueryOperator.Equals, "2026-08-21");
        var compilation = QueryCompiler.Compile(TaskFieldCatalog.Instance, group, context);

        compilation.Predicate.Should().Be("(pt.created_at >= @q0 AND pt.created_at < @q1)");
        Read<DateTime>(compilation, "q0").Should().Be(new DateTime(2026, 8, 20, 14, 0, 0, DateTimeKind.Utc));
        Read<DateTime>(compilation, "q1").Should().Be(new DateTime(2026, 8, 21, 14, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void Compile_CreatedAtLessThanOrEqual_IncludesTheWholeDay()
    {
        var compilation = Compile(TaskFieldKeys.CreatedAt, QueryOperator.LessThanOrEqual, "2026-08-21");

        compilation.Predicate.Should().Be("pt.created_at < @q0");
        Read<DateTime>(compilation, "q0").Should().Be(new DateTime(2026, 8, 22, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void Compile_TagsIn_EmitsTheSameShapeTheTaskFilterUses()
    {
        var compilation = Compile(TaskFieldKeys.Tags, QueryOperator.In, "regression");

        compilation.Predicate.Should().Contain("EXISTS (");
        compilation.Predicate.Should().Contain("FROM project_task_tags q_ptt");
        compilation.Predicate.Should().Contain("INNER JOIN tags q_t ON q_ptt.tag_id = q_t.id AND NOT q_t.is_deleted");
        compilation.Predicate.Should().Contain("q_ptt.project_task_id = pt.id");
        compilation.Predicate.Should().Contain("AND q_t.name = ANY(@q0)");
        Read<string[]>(compilation, "q0").Should().Equal("regression");
    }

    [Fact]
    public void Compile_TagsIsEmpty_NegatesTheExistenceCheck()
    {
        var compilation = Compile(TaskFieldKeys.Tags, QueryOperator.IsEmpty);

        compilation.Predicate.Should().StartWith("NOT EXISTS (");
        compilation.Predicate.Should().NotContain("q_t.name");
    }

    [Fact]
    public void Compile_AssigneesIn_MatchesUserIds()
    {
        var compilation = Compile(TaskFieldKeys.Assignees, QueryOperator.In, "user-1", "user-2");

        compilation.Predicate.Should().Contain("FROM project_task_app_users q_ptau");
        compilation.Predicate.Should().Contain("AND q_ptau.user_id = ANY(@q0)");
        Read<string[]>(compilation, "q0").Should().Equal("user-1", "user-2");
    }

    [Fact]
    public void Compile_FlagsIsNotEmpty_ScopesToTheTaskEntityType()
    {
        var compilation = Compile(TaskFieldKeys.Flags, QueryOperator.IsNotEmpty);

        compilation.Predicate.Should().Contain("FROM flags q_f");
        compilation.Predicate.Should().Contain("q_f.entity_type = @q0");
        Read<int>(compilation, "q0").Should().Be((int)EntityType.Task);
    }

    [Fact]
    public void Compile_CommentsIsEmpty_ExcludesDeletedComments()
    {
        var compilation = Compile(TaskFieldKeys.Comments, QueryOperator.IsEmpty);

        compilation.Predicate.Should().StartWith("NOT EXISTS (");
        compilation.Predicate.Should().Contain("NOT q_c.is_deleted");
    }

    [Fact]
    public void Compile_RelationsIn_SeparatesTheDirections()
    {
        var compilation = Compile(TaskFieldKeys.Relations, QueryOperator.In, "3:source", "4:target");

        compilation.Predicate.Should().Contain("q_ptr.relation_type_id = ANY(@q0) AND q_ptr.source_task_id = pt.id");
        compilation.Predicate.Should().Contain("q_ptr.relation_type_id = ANY(@q1) AND q_ptr.target_task_id = pt.id");
        Read<int[]>(compilation, "q0").Should().Equal(3);
        Read<int[]>(compilation, "q1").Should().Equal(4);
    }

    [Fact]
    public void Compile_RelationsWithoutADirection_MatchesEitherEnd()
    {
        var compilation = Compile(TaskFieldKeys.Relations, QueryOperator.In, "3");

        compilation.Predicate.Should().Contain("(q_ptr.relation_type_id = ANY(@q0))");
        compilation.Predicate.Should().NotContain("source_task_id = pt.id)");
    }

    [Fact]
    public void Compile_NestedGroups_ParenthesiseAndKeepParametersDistinct()
    {
        var group = new QueryGroup
        {
            Operator = QueryGroupOperator.All,
            Conditions =
            [
                new QueryCondition
                {
                    Field = TaskFieldKeys.DueDate,
                    Operator = QueryOperator.InNextDays,
                    Values = ["7"],
                },
            ],
            Groups =
            [
                new QueryGroup
                {
                    Operator = QueryGroupOperator.Any,
                    Conditions =
                    [
                        new QueryCondition
                        {
                            Field = TaskFieldKeys.Priority,
                            Operator = QueryOperator.Equals,
                            Values = ["3"],
                        },
                        new QueryCondition
                        {
                            Field = TaskFieldKeys.Tags,
                            Operator = QueryOperator.In,
                            Values = ["regression"],
                        },
                    ],
                },
            ],
        };
        var compilation = QueryCompiler.Compile(TaskFieldCatalog.Instance, group, Context);

        compilation.Predicate.Should().StartWith("((pt.due_date >= @q0 AND pt.due_date <= @q1) AND (pt.priority = @q2 OR EXISTS (");
        compilation.Predicate.Should().EndWith("))");
        compilation.Parameters.ParameterNames.Should().OnlyHaveUniqueItems();
        compilation.Parameters.ParameterNames.Should().HaveCount(4);
    }

    [Fact]
    public void Compile_NoneGroup_NegatesTheDisjunction()
    {
        var group = new QueryGroup
        {
            Operator = QueryGroupOperator.None,
            Conditions =
            [
                new QueryCondition
                {
                    Field = TaskFieldKeys.Status,
                    Operator = QueryOperator.Equals,
                    Values = ["4"],
                },
                new QueryCondition
                {
                    Field = TaskFieldKeys.Priority,
                    Operator = QueryOperator.Equals,
                    Values = ["1"],
                },
            ],
        };
        var compilation = QueryCompiler.Compile(TaskFieldCatalog.Instance, group, Context);

        compilation.Predicate.Should().Be("NOT (pt.status_id = @q0 OR pt.priority = @q1)");
    }

    [Fact]
    public void Compile_NestedEmptyGroup_MatchesNothingWithoutFailingTheQuery()
    {
        var group = new QueryGroup
        {
            Operator = QueryGroupOperator.All,
            Conditions =
            [
                new QueryCondition
                {
                    Field = TaskFieldKeys.Status,
                    Operator = QueryOperator.Equals,
                    Values = ["4"],
                },
            ],
            Groups = [new QueryGroup()],
        };
        var compilation = QueryCompiler.Compile(TaskFieldCatalog.Instance, group, Context);

        compilation.Predicate.Should().Be("(pt.status_id = @q0 AND FALSE)");
    }

    // Compilation binds values the validator is assumed to have accepted. Skipping that step used to
    // surface as an InvalidCastException on a set membership, or as a predicate quietly built from a
    // default date; these pin the loud failure instead.
    [Theory]
    [InlineData(QueryOperator.Equals, "not-a-number")]
    [InlineData(QueryOperator.GreaterThan, "not-a-number")]
    public void Compile_AnUnvalidatedScalarValue_FailsLoudly(QueryOperator queryOperator, string value)
    {
        var group = GroupFor(TaskFieldKeys.EstimateValue, queryOperator, value);
        var compile = () => QueryCompiler.Compile(TaskFieldCatalog.Instance, group, Context);

        compile.Should().Throw<QueryCompilationException>().WithMessage($"*{value}*");
    }

    [Fact]
    public void Compile_AnUnvalidatedDayCount_FailsLoudlyRatherThanDefaultingToZero()
    {
        var group = GroupFor(TaskFieldKeys.DueDate, QueryOperator.InNextDays, "soon");
        var compile = () => QueryCompiler.Compile(TaskFieldCatalog.Instance, group, Context);

        compile.Should().Throw<QueryCompilationException>().WithMessage("*soon*");
    }

    [Fact]
    public void Compile_AnUnvalidatedValueInASet_FailsLoudlyRatherThanFailingToCast()
    {
        var group = GroupFor(TaskFieldKeys.Status, QueryOperator.In, "4", "not-a-status");
        var compile = () => QueryCompiler.Compile(TaskFieldCatalog.Instance, group, Context);

        compile.Should().Throw<QueryCompilationException>();
    }

    [Theory]
    [InlineData(QueryOperator.Equals, "whenever")]
    [InlineData(QueryOperator.Between, "whenever")]
    [InlineData(QueryOperator.InLastDays, "a while")]
    public void Compile_AnUnvalidatedTimestampValue_FailsLoudly(QueryOperator queryOperator, string value)
    {
        var values = queryOperator == QueryOperator.Between ? new[] { value, value } : [value];
        var group = GroupFor(TaskFieldKeys.CreatedAt, queryOperator, values);
        var compile = () => QueryCompiler.Compile(TaskFieldCatalog.Instance, group, Context);

        compile.Should().Throw<QueryCompilationException>();
    }

    [Fact]
    public void EveryCustomParsedField_HasASample()
    {
        var customParsed = TaskFieldCatalog.Instance.Fields
            .Where(field => field.ValueParser is not null)
            .Select(field => field.Key);

        CustomParsedSamples.Keys.Should().Contain(customParsed);
    }

    [Fact]
    public void Compile_EveryCatalogFieldAndOperator_ProducesAPredicate()
    {
        foreach (var field in TaskFieldCatalog.Instance.Fields)
        {
            foreach (var queryOperator in field.Operators)
            {
                var group = GroupFor(field.Key, queryOperator, SampleValues(field, queryOperator));
                var compile = () => QueryCompiler.Compile(TaskFieldCatalog.Instance, group, Context);

                compile.Should().NotThrow($"{field.Key} declares {queryOperator}");
                compile().Predicate.Should().NotBeNullOrWhiteSpace();
            }
        }
    }

    // Fields declaring their own IQueryValueParser accept a shape the parameter type cannot describe,
    // so each needs a sample here. EveryCustomParsedField_HasASample keeps this honest.
    private static readonly Dictionary<string, string> CustomParsedSamples = new()
    {
        [TaskFieldKeys.Relations] = "1:source",
    };

    private static string[] SampleValues(QueryField field, QueryOperator queryOperator)
    {
        if (queryOperator is QueryOperator.IsEmpty or QueryOperator.IsNotEmpty or QueryOperator.IsOverdue)
        {
            return [];
        }

        if (queryOperator is QueryOperator.InNextDays or QueryOperator.InLastDays)
        {
            return ["7"];
        }

        var value = CustomParsedSamples.TryGetValue(field.Key, out var custom)
            ? custom
            : field.ParameterType switch
            {
                QueryParameterType.Integer => field.EnumType is null ? "1" : "0",
                QueryParameterType.Decimal => "1.5",
                QueryParameterType.Date => "2026-08-21",
                QueryParameterType.Timestamp => "2026-08-21",
                _ => "sample",
            };

        if (queryOperator is QueryOperator.Between)
        {
            return [value, value];
        }

        return [value];
    }

    private static QueryCompilation Compile(string fieldKey, QueryOperator queryOperator, params string[] values)
    {
        return QueryCompiler.Compile(TaskFieldCatalog.Instance, GroupFor(fieldKey, queryOperator, values), Context);
    }

    private static QueryGroup GroupFor(string fieldKey, QueryOperator queryOperator, params string[] values)
    {
        return new QueryGroup
        {
            Operator = QueryGroupOperator.All,
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
    }

    private static TValue Read<TValue>(QueryCompilation compilation, string name)
    {
        return compilation.Parameters.Get<TValue>(name);
    }
}
