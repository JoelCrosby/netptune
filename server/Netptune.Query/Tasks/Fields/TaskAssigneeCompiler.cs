using Netptune.Query.Compilation;
using Netptune.Query.Model;

namespace Netptune.Query.Tasks.Fields;

internal sealed class TaskAssigneeCompiler : IQueryFieldCompiler
{
    private const string Subquery = """
        SELECT 1
                  FROM project_task_app_users q_ptau
                  WHERE q_ptau.project_task_id = pt.id
        """;

    public string Compile(QueryCompileRequest request, QueryParameterBag parameters)
    {
        var condition = request.Condition;

        switch (condition.Operator)
        {
            case QueryOperator.In:
                {
                    var parameter = parameters.Add(condition.Values.ToArray());

                    return $"EXISTS ({Subquery} AND q_ptau.user_id = ANY({parameter}))";
                }

            case QueryOperator.NotIn:
                {
                    var parameter = parameters.Add(condition.Values.ToArray());

                    return $"NOT EXISTS ({Subquery} AND q_ptau.user_id = ANY({parameter}))";
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
