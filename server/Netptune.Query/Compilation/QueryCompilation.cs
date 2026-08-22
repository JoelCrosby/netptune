using Dapper;

namespace Netptune.Query.Compilation;

public sealed record QueryCompilation
{
    public required string Predicate { get; init; }

    public required DynamicParameters Parameters { get; init; }
}
