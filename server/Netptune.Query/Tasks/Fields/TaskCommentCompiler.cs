using Netptune.Core.Enums;
using Netptune.Query.Compilation;
using Netptune.Query.Model;

namespace Netptune.Query.Tasks.Fields;

internal sealed class TaskCommentCompiler : IQueryFieldCompiler
{
    public string Compile(QueryCompileRequest request, QueryParameterBag parameters)
    {
        var condition = request.Condition;
        var entityType = parameters.Add((int)EntityType.Task);
        var subquery = $"""
            SELECT 1
                      FROM comments q_c
                      WHERE q_c.entity_type = {entityType}
                        AND q_c.entity_id = pt.id
                        AND NOT q_c.is_deleted
            """;

        return condition.Operator switch
        {
            QueryOperator.IsEmpty => $"NOT EXISTS ({subquery})",
            QueryOperator.IsNotEmpty => $"EXISTS ({subquery})",
            _ => throw new QueryCompilationException(request.Field.Key, condition.Operator),
        };
    }
}
