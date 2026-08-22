using Netptune.Query.Compilation;
using Netptune.Query.Model;
using Netptune.Query.Schema;

namespace Netptune.Query.Compilation.Fields;

internal sealed class TimestampFieldCompiler : IQueryFieldCompiler
{
    public required string Column { get; init; }

    public string Compile(QueryCompileRequest request, QueryParameterBag parameters)
    {
        var field = request.Field;
        var condition = request.Condition;
        var context = request.Context;

        switch (condition.Operator)
        {
            case QueryOperator.Equals:
                return CompileDayRange(ParseDate(field, condition.Values[0]), 1, parameters, context, false);

            case QueryOperator.NotEquals:
                return CompileDayRange(ParseDate(field, condition.Values[0]), 1, parameters, context, true);

            case QueryOperator.GreaterThan:
                {
                    var parameter = AddInstant(parameters, context, ParseDate(field, condition.Values[0]).AddDays(1));

                    return $"{Column} >= {parameter}";
                }

            case QueryOperator.GreaterThanOrEqual:
                {
                    var parameter = AddInstant(parameters, context, ParseDate(field, condition.Values[0]));

                    return $"{Column} >= {parameter}";
                }

            case QueryOperator.LessThan:
                {
                    var parameter = AddInstant(parameters, context, ParseDate(field, condition.Values[0]));

                    return $"{Column} < {parameter}";
                }

            case QueryOperator.LessThanOrEqual:
                {
                    var parameter = AddInstant(parameters, context, ParseDate(field, condition.Values[0]).AddDays(1));

                    return $"{Column} < {parameter}";
                }

            case QueryOperator.Between:
                {
                    var start = ParseDate(field, condition.Values[0]);
                    var end = ParseDate(field, condition.Values[1]);
                    var lower = AddInstant(parameters, context, start);
                    var upper = AddInstant(parameters, context, end.AddDays(1));

                    return $"({Column} >= {lower} AND {Column} < {upper})";
                }

            case QueryOperator.IsEmpty:
                return $"{Column} IS NULL";

            case QueryOperator.IsNotEmpty:
                return $"{Column} IS NOT NULL";

            case QueryOperator.InNextDays:
                {
                    var days = ParseDayCount(condition);

                    var lower = AddInstant(parameters, context, context.Today);
                    var upper = AddInstant(parameters, context, context.Today.AddDays(days + 1));

                    return $"({Column} >= {lower} AND {Column} < {upper})";
                }

            case QueryOperator.InLastDays:
                {
                    var days = ParseDayCount(condition);

                    var lower = AddInstant(parameters, context, context.Today.AddDays(-days));
                    var upper = AddInstant(parameters, context, context.Today.AddDays(1));

                    return $"({Column} >= {lower} AND {Column} < {upper})";
                }

            default:
                throw new QueryCompilationException(field.Key, condition.Operator);
        }
    }

    private string CompileDayRange(
        DateOnly day,
        int dayCount,
        QueryParameterBag parameters,
        QueryCompilationContext context,
        bool isNegated)
    {
        var lower = AddInstant(parameters, context, day);
        var upper = AddInstant(parameters, context, day.AddDays(dayCount));

        if (isNegated)
        {
            return $"({Column} IS NULL OR {Column} < {lower} OR {Column} >= {upper})";
        }

        return $"({Column} >= {lower} AND {Column} < {upper})";
    }

    private static string AddInstant(QueryParameterBag parameters, QueryCompilationContext context, DateOnly day)
    {
        var instant = context.ToInstant(day);

        return parameters.Add(instant);
    }

    private static DateOnly ParseDate(QueryField field, string value)
    {
        var isBound = QueryValueBinder.TryParse(field, value, out var parsed);

        if (!isBound || parsed is not DateOnly date)
        {
            throw QueryCompilationException.UnboundValue(field.Key, value);
        }

        return date;
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
}
