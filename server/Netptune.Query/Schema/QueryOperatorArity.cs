using Netptune.Query.Model;

namespace Netptune.Query.Schema;

public enum QueryArity
{
    None,
    One,
    Two,
    AtLeastOne,
}

public static class QueryOperatorArity
{
    public static QueryArity For(QueryOperator queryOperator)
    {
        return queryOperator switch
        {
            QueryOperator.IsEmpty => QueryArity.None,
            QueryOperator.IsNotEmpty => QueryArity.None,
            QueryOperator.IsOverdue => QueryArity.None,
            QueryOperator.Between => QueryArity.Two,
            QueryOperator.In => QueryArity.AtLeastOne,
            QueryOperator.NotIn => QueryArity.AtLeastOne,
            _ => QueryArity.One,
        };
    }

    public static bool IsSatisfiedBy(QueryArity arity, int valueCount)
    {
        return arity switch
        {
            QueryArity.None => valueCount == 0,
            QueryArity.One => valueCount == 1,
            QueryArity.Two => valueCount == 2,
            _ => valueCount >= 1,
        };
    }
}
