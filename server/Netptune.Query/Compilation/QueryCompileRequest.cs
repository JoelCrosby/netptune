using Netptune.Query.Model;
using Netptune.Query.Schema;

namespace Netptune.Query.Compilation;

public sealed record QueryCompileRequest
{
    public required QueryField Field { get; init; }

    public required QueryCondition Condition { get; init; }

    public required QueryCompilationContext Context { get; init; }
}
