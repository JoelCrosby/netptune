using Netptune.Query.Compilation;
using Netptune.Query.Model;

namespace Netptune.Query.Tasks.Fields;

internal sealed class TaskBoardCompiler : IQueryFieldCompiler
{
    private const string Subquery = """
        SELECT 1
                  FROM project_task_in_board_groups q_ptibg
                           INNER JOIN board_groups q_bg ON q_ptibg.board_group_id = q_bg.id AND NOT q_bg.is_deleted
                           INNER JOIN boards q_b ON q_bg.board_id = q_b.id AND NOT q_b.is_deleted
                  WHERE q_ptibg.project_task_id = pt.id
        """;

    public string Compile(QueryCompileRequest request, QueryParameterBag parameters)
    {
        var condition = request.Condition;

        switch (condition.Operator)
        {
            case QueryOperator.In:
                {
                    var parameter = parameters.Add(condition.Values.Select(int.Parse).ToArray());

                    return $"EXISTS ({Subquery} AND q_b.id = ANY({parameter}))";
                }

            case QueryOperator.NotIn:
                {
                    var parameter = parameters.Add(condition.Values.Select(int.Parse).ToArray());

                    return $"NOT EXISTS ({Subquery} AND q_b.id = ANY({parameter}))";
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
