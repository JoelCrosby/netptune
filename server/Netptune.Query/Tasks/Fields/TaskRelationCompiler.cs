using Netptune.Query.Compilation;
using Netptune.Query.Model;

namespace Netptune.Query.Tasks.Fields;

internal sealed class TaskRelationCompiler : IQueryFieldCompiler
{
    public string Compile(QueryCompileRequest request, QueryParameterBag parameters)
    {
        var condition = request.Condition;

        switch (condition.Operator)
        {
            case QueryOperator.In:
                return $"EXISTS ({BuildTypedSubquery(condition, parameters)})";

            case QueryOperator.NotIn:
                return $"NOT EXISTS ({BuildTypedSubquery(condition, parameters)})";

            case QueryOperator.IsEmpty:
                return $"NOT EXISTS ({BuildSubquery("TRUE")})";

            case QueryOperator.IsNotEmpty:
                return $"EXISTS ({BuildSubquery("TRUE")})";

            default:
                throw new QueryCompilationException(request.Field.Key, condition.Operator);
        }
    }

    private static string BuildTypedSubquery(QueryCondition condition, QueryParameterBag parameters)
    {
        var references = condition.Values
            .Select(TaskRelationReference.Parse)
            .Where(reference => reference is not null)
            .Select(reference => reference!)
            .ToList();
        var clauses = new List<string>();

        AddDirectionClause(clauses, references, TaskRelationDirection.Any, parameters);
        AddDirectionClause(clauses, references, TaskRelationDirection.Source, parameters);
        AddDirectionClause(clauses, references, TaskRelationDirection.Target, parameters);

        if (clauses.Count == 0)
        {
            return BuildSubquery("FALSE");
        }

        var match = string.Join(" OR ", clauses);

        return BuildSubquery($"({match})");
    }

    private static void AddDirectionClause(
        List<string> clauses,
        List<TaskRelationReference> references,
        TaskRelationDirection direction,
        QueryParameterBag parameters)
    {
        var relationTypeIds = references
            .Where(reference => reference.Direction == direction)
            .Select(reference => reference.RelationTypeId)
            .Distinct()
            .ToArray();

        if (relationTypeIds.Length == 0)
        {
            return;
        }

        var parameter = parameters.Add(relationTypeIds);
        var typeMatch = $"q_ptr.relation_type_id = ANY({parameter})";
        var clause = direction switch
        {
            TaskRelationDirection.Source => $"({typeMatch} AND q_ptr.source_task_id = pt.id)",
            TaskRelationDirection.Target => $"({typeMatch} AND q_ptr.target_task_id = pt.id)",
            _ => $"({typeMatch})",
        };

        clauses.Add(clause);
    }

    private static string BuildSubquery(string match)
    {
        return $"""
            SELECT 1
                      FROM project_task_relations q_ptr
                               INNER JOIN relation_types q_rt ON q_ptr.relation_type_id = q_rt.id AND NOT q_rt.is_deleted
                               INNER JOIN project_tasks q_other
                                          ON q_other.id = CASE
                                                              WHEN q_ptr.source_task_id = pt.id THEN q_ptr.target_task_id
                                                              ELSE q_ptr.source_task_id
                                              END
                                              AND NOT q_other.is_deleted
                      WHERE q_ptr.workspace_id = pt.workspace_id
                        AND (q_ptr.source_task_id = pt.id OR q_ptr.target_task_id = pt.id)
                        AND {match}
            """;
    }
}
