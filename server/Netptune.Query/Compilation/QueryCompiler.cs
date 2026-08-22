using Netptune.Query.Model;
using Netptune.Query.Schema;

namespace Netptune.Query.Compilation;

public static class QueryCompiler
{
    public const string MatchesEverything = "TRUE";
    public const string MatchesNothing = "FALSE";

    public static QueryCompilation Compile(
        IQueryFieldCatalog catalog,
        QueryGroup? group,
        QueryCompilationContext context)
    {
        var parameters = new QueryParameterBag();
        var scope = new CompilationScope(catalog, parameters, context);
        var predicate = group is null ? MatchesEverything : CompileGroup(group, scope);

        return new QueryCompilation
        {
            Predicate = predicate,
            Parameters = parameters.Build(),
        };
    }

    private static string CompileGroup(QueryGroup group, CompilationScope scope)
    {
        if (group.IsEmpty())
        {
            return MatchesNothing;
        }

        var conditionPredicates = group.Conditions.Select(condition => CompileCondition(condition, scope));
        var nestedPredicates = group.Groups.Select(nested => CompileGroup(nested, scope));
        var members = conditionPredicates.Concat(nestedPredicates).ToList();
        var isSingleMember = members.Count == 1;

        return group.Operator switch
        {
            QueryGroupOperator.All => isSingleMember ? members[0] : $"({string.Join(" AND ", members)})",
            QueryGroupOperator.Any => isSingleMember ? members[0] : $"({string.Join(" OR ", members)})",
            QueryGroupOperator.None => $"NOT ({string.Join(" OR ", members)})",
            _ => MatchesNothing,
        };
    }

    private static string CompileCondition(QueryCondition condition, CompilationScope scope)
    {
        var field = scope.Catalog.Find(condition.Field);

        if (field is null)
        {
            throw new QueryCompilationException(condition.Field, condition.Operator);
        }

        var request = new QueryCompileRequest
        {
            Field = field,
            Condition = condition,
            Context = scope.Context,
        };

        return field.Compiler.Compile(request, scope.Parameters);
    }

    private sealed record CompilationScope(
        IQueryFieldCatalog Catalog,
        QueryParameterBag Parameters,
        QueryCompilationContext Context);
}
