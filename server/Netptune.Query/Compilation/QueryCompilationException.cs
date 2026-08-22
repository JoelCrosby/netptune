using Netptune.Query.Model;

namespace Netptune.Query.Compilation;

public sealed class QueryCompilationException : Exception
{
    public QueryCompilationException(string fieldKey, QueryOperator queryOperator)
        : base($"Operator '{queryOperator}' is not supported for field '{fieldKey}'.")
    {
    }

    private QueryCompilationException(string message) : base(message)
    {
    }

    public static QueryCompilationException UnboundValue(string fieldKey, string value)
    {
        return new QueryCompilationException($"Value '{value}' on field '{fieldKey}' is not valid for that field and cannot be compiled.");
    }
}
