using Netptune.Query.Compilation;
using Netptune.Query.Model;

namespace Netptune.Query.Schema;

public sealed record QueryField
{
    public required string Key { get; init; }

    public required string Name { get; init; }

    public required QueryValueType ValueType { get; init; }

    public required IReadOnlyList<QueryOperator> Operators { get; init; }

    public required QueryParameterType ParameterType { get; init; }

    public required IQueryFieldCompiler Compiler { get; init; }

    public IQueryValueParser? ValueParser { get; init; }

    public string? OptionSource { get; init; }

    public Type? EnumType { get; init; }

    public bool IsMultiValued { get; init; }

    public string? SortKey { get; init; }

    public bool IsSortable => SortKey is not null;

    public bool Supports(QueryOperator queryOperator)
    {
        return Operators.Contains(queryOperator);
    }
}
