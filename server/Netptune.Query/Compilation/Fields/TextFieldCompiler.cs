using Netptune.Query.Compilation;
using Netptune.Query.Model;

namespace Netptune.Query.Compilation.Fields;

internal sealed class TextFieldCompiler : IQueryFieldCompiler
{
    public required string Column { get; init; }

    public string Compile(QueryCompileRequest request, QueryParameterBag parameters)
    {
        var condition = request.Condition;
        var lowered = $"LOWER({Column})";

        switch (condition.Operator)
        {
            case QueryOperator.Equals:
                {
                    var parameter = parameters.Add(Normalize(condition.Values[0]));

                    return $"{lowered} = {parameter}";
                }

            case QueryOperator.NotEquals:
                {
                    var parameter = parameters.Add(Normalize(condition.Values[0]));

                    return $"({Column} IS NULL OR {lowered} <> {parameter})";
                }

            case QueryOperator.In:
                {
                    var parameter = parameters.Add(NormalizeAll(condition));

                    return $"{lowered} = ANY({parameter})";
                }

            case QueryOperator.NotIn:
                {
                    var parameter = parameters.Add(NormalizeAll(condition));

                    return $"({Column} IS NULL OR NOT ({lowered} = ANY({parameter})))";
                }

            case QueryOperator.Contains:
                {
                    var parameter = parameters.Add(SqlPatterns.Contains(Normalize(condition.Values[0])));

                    return $"{lowered} LIKE {parameter}{SqlPatterns.LikeEscape}";
                }

            case QueryOperator.NotContains:
                {
                    var parameter = parameters.Add(SqlPatterns.Contains(Normalize(condition.Values[0])));

                    return $"({Column} IS NULL OR {lowered} NOT LIKE {parameter}{SqlPatterns.LikeEscape})";
                }

            case QueryOperator.StartsWith:
                {
                    var parameter = parameters.Add(SqlPatterns.StartsWith(Normalize(condition.Values[0])));

                    return $"{lowered} LIKE {parameter}{SqlPatterns.LikeEscape}";
                }

            case QueryOperator.IsEmpty:
                return $"({Column} IS NULL OR {Column} = '')";

            case QueryOperator.IsNotEmpty:
                return $"({Column} IS NOT NULL AND {Column} <> '')";

            default:
                throw new QueryCompilationException(request.Field.Key, condition.Operator);
        }
    }

    private static string Normalize(string value)
    {
        return value.Trim().ToLowerInvariant();
    }

    private static string[] NormalizeAll(QueryCondition condition)
    {
        return condition.Values.Select(Normalize).ToArray();
    }
}
