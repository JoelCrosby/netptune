using Netptune.Core.Enums;
using Netptune.Query.Model;
using Netptune.Query.Schema;

namespace Netptune.Query.Compilation.Fields;

internal sealed class ScalarFieldCompiler : IQueryFieldCompiler
{
    public required string Column { get; init; }

    public bool MatchesNullOnZero { get; init; }

    public string Compile(QueryCompileRequest request, QueryParameterBag parameters)
    {
        var field = request.Field;
        var condition = request.Condition;

        switch (condition.Operator)
        {
            case QueryOperator.Equals:
                return CompileEquality(field, condition, parameters, false);

            case QueryOperator.NotEquals:
                return CompileEquality(field, condition, parameters, true);

            case QueryOperator.In:
                return CompileMembership(field, condition, parameters, false);

            case QueryOperator.NotIn:
                return CompileMembership(field, condition, parameters, true);

            case QueryOperator.GreaterThan:
                return CompileComparison(field, condition, parameters, ">");

            case QueryOperator.GreaterThanOrEqual:
                return CompileComparison(field, condition, parameters, ">=");

            case QueryOperator.LessThan:
                return CompileComparison(field, condition, parameters, "<");

            case QueryOperator.LessThanOrEqual:
                return CompileComparison(field, condition, parameters, "<=");

            case QueryOperator.Between:
                {
                    var lower = parameters.Add(Parse(field, condition.Values[0]));
                    var upper = parameters.Add(Parse(field, condition.Values[1]));

                    return $"({Column} >= {lower} AND {Column} <= {upper})";
                }

            case QueryOperator.IsEmpty:
                return $"{Column} IS NULL";

            case QueryOperator.IsNotEmpty:
                return $"{Column} IS NOT NULL";

            case QueryOperator.InNextDays:
                return CompileRelativeRange(condition, parameters, request.Context, true);

            case QueryOperator.InLastDays:
                return CompileRelativeRange(condition, parameters, request.Context, false);

            case QueryOperator.IsOverdue:
                {
                    var today = parameters.Add(request.Context.Today);
                    var doneCategory = parameters.Add((int)StatusCategory.Done);

                    return $"({Column} IS NOT NULL AND {Column} < {today} AND st.category <> {doneCategory})";
                }

            default:
                throw new QueryCompilationException(field.Key, condition.Operator);
        }
    }

    private string CompileEquality(
        QueryField field,
        QueryCondition condition,
        QueryParameterBag parameters,
        bool isNegated)
    {
        var value = Parse(field, condition.Values[0]);
        var parameter = parameters.Add(value);
        var alsoMatchesNull = MatchesNullOnZero && IsZero(value);

        if (isNegated)
        {
            if (alsoMatchesNull)
            {
                return $"({Column} IS NOT NULL AND {Column} <> {parameter})";
            }

            return $"({Column} IS NULL OR {Column} <> {parameter})";
        }

        if (alsoMatchesNull)
        {
            return $"({Column} = {parameter} OR {Column} IS NULL)";
        }

        return $"{Column} = {parameter}";
    }

    private string CompileMembership(
        QueryField field,
        QueryCondition condition,
        QueryParameterBag parameters,
        bool isNegated)
    {
        var values = condition.Values.Select(value => Parse(field, value)).ToList();
        var parameter = parameters.Add(ToTypedArray(field, values));
        var alsoMatchesNull = MatchesNullOnZero && values.Any(IsZero);

        if (isNegated)
        {
            if (alsoMatchesNull)
            {
                return $"({Column} IS NOT NULL AND NOT ({Column} = ANY({parameter})))";
            }

            return $"({Column} IS NULL OR NOT ({Column} = ANY({parameter})))";
        }

        if (alsoMatchesNull)
        {
            return $"({Column} = ANY({parameter}) OR {Column} IS NULL)";
        }

        return $"{Column} = ANY({parameter})";
    }

    private string CompileComparison(
        QueryField field,
        QueryCondition condition,
        QueryParameterBag parameters,
        string comparison)
    {
        var parameter = parameters.Add(Parse(field, condition.Values[0]));

        return $"{Column} {comparison} {parameter}";
    }

    private string CompileRelativeRange(
        QueryCondition condition,
        QueryParameterBag parameters,
        QueryCompilationContext context,
        bool isForward)
    {
        var days = ParseDayCount(condition);
        var today = context.Today;
        var offset = isForward ? today.AddDays(days) : today.AddDays(-days);
        var lower = parameters.Add(isForward ? today : offset);
        var upper = parameters.Add(isForward ? offset : today);

        return $"({Column} >= {lower} AND {Column} <= {upper})";
    }

    private static int ParseDayCount(QueryCondition condition)
    {
        var value = condition.Values[0];

        if (!QueryValueBinder.TryParseDayCount(value, out var days))
        {
            throw QueryCompilationException.UnboundValue(condition.Field, value);
        }

        return days;
    }

    private static bool IsZero(object? value)
    {
        return value is int and 0;
    }

    private static object Parse(QueryField field, string value)
    {
        var isBound = QueryValueBinder.TryParse(field, value, out var parsed);

        if (!isBound || parsed is null)
        {
            throw QueryCompilationException.UnboundValue(field.Key, value);
        }

        return parsed;
    }

    private static Array ToTypedArray(QueryField field, List<object> parsed)
    {
        return field.ParameterType switch
        {
            QueryParameterType.Text => parsed.Cast<string>().ToArray(),
            QueryParameterType.Decimal => parsed.Cast<decimal>().ToArray(),
            QueryParameterType.Date => parsed.Cast<DateOnly>().ToArray(),
            _ => parsed.Cast<int>().ToArray(),
        };
    }
}
