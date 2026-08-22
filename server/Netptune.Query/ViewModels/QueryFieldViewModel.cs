using Netptune.Core.Enums;
using Netptune.Query.Model;
using Netptune.Query.Schema;

namespace Netptune.Query.ViewModels;

public sealed record QueryFieldViewModel
{
    public required string Key { get; init; }

    public required string Name { get; init; }

    public QueryValueType ValueType { get; init; }

    public required IReadOnlyList<QueryOperator> Operators { get; init; }

    public string? OptionSource { get; init; }

    public bool IsMultiValued { get; init; }

    public bool IsSortable { get; init; }

    public string? SortKey { get; init; }
}
