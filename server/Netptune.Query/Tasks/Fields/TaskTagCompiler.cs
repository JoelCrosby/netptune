using Netptune.Query.Compilation;
using Netptune.Query.Model;

namespace Netptune.Query.Tasks.Fields;

internal sealed class TaskTagCompiler : IQueryFieldCompiler
{
    private const string Subquery = """
        SELECT 1
                  FROM project_task_tags q_ptt
                           INNER JOIN tags q_t ON q_ptt.tag_id = q_t.id AND NOT q_t.is_deleted
                  WHERE q_ptt.project_task_id = pt.id
        """;

    public string Compile(QueryCompileRequest request, QueryParameterBag parameters)
    {
        var condition = request.Condition;

        switch (condition.Operator)
        {
            case QueryOperator.In:
                {
                    var parameter = parameters.Add(condition.Values.ToArray());

                    return $"EXISTS ({Subquery} AND q_t.name = ANY({parameter}))";
                }

            case QueryOperator.NotIn:
                {
                    var parameter = parameters.Add(condition.Values.ToArray());

                    return $"NOT EXISTS ({Subquery} AND q_t.name = ANY({parameter}))";
                }

            case QueryOperator.IsEmpty:
                return $"NOT EXISTS ({Subquery})";

            case QueryOperator.IsNotEmpty:
                return $"EXISTS ({Subquery})";

            default:
                throw new QueryCompilationException(request.Field.Key, condition.Operator);
        }
    }
}
