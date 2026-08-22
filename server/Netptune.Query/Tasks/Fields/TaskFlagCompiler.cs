using Netptune.Core.Enums;
using Netptune.Query.Compilation;
using Netptune.Query.Model;

namespace Netptune.Query.Tasks.Fields;

internal sealed class TaskFlagCompiler : IQueryFieldCompiler
{
    public string Compile(QueryCompileRequest request, QueryParameterBag parameters)
    {
        var condition = request.Condition;
        var entityType = parameters.Add((int)EntityType.Task);
        var subquery = $"""
            SELECT 1
                      FROM flags q_f
                      WHERE q_f.workspace_id = pt.workspace_id
                        AND q_f.entity_type = {entityType}
                        AND q_f.entity_id = pt.id
                        AND NOT q_f.is_deleted
            """;

        return condition.Operator switch
        {
            QueryOperator.IsEmpty => $"NOT EXISTS ({subquery})",
            QueryOperator.IsNotEmpty => $"EXISTS ({subquery})",
            _ => throw new QueryCompilationException(request.Field.Key, condition.Operator),
        };
    }
}
