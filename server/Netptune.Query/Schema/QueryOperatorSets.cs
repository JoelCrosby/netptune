using Netptune.Query.Model;

namespace Netptune.Query.Schema;

internal static class QueryOperatorSets
{
    public static readonly QueryOperator[] Text =
    [
        QueryOperator.Equals,
        QueryOperator.NotEquals,
        QueryOperator.In,
        QueryOperator.NotIn,
        QueryOperator.Contains,
        QueryOperator.NotContains,
        QueryOperator.StartsWith,
        QueryOperator.IsEmpty,
        QueryOperator.IsNotEmpty,
    ];

    public static readonly QueryOperator[] Enumeration =
    [
        QueryOperator.Equals,
        QueryOperator.NotEquals,
        QueryOperator.In,
        QueryOperator.NotIn,
    ];

    public static readonly QueryOperator[] NullableEnumeration =
    [
        QueryOperator.Equals,
        QueryOperator.NotEquals,
        QueryOperator.In,
        QueryOperator.NotIn,
        QueryOperator.IsEmpty,
        QueryOperator.IsNotEmpty,
    ];

    public static readonly QueryOperator[] Number =
    [
        QueryOperator.Equals,
        QueryOperator.NotEquals,
        QueryOperator.GreaterThan,
        QueryOperator.GreaterThanOrEqual,
        QueryOperator.LessThan,
        QueryOperator.LessThanOrEqual,
        QueryOperator.Between,
        QueryOperator.IsEmpty,
        QueryOperator.IsNotEmpty,
    ];

    public static readonly QueryOperator[] Date =
    [
        QueryOperator.Equals,
        QueryOperator.NotEquals,
        QueryOperator.GreaterThan,
        QueryOperator.GreaterThanOrEqual,
        QueryOperator.LessThan,
        QueryOperator.LessThanOrEqual,
        QueryOperator.Between,
        QueryOperator.IsEmpty,
        QueryOperator.IsNotEmpty,
        QueryOperator.InNextDays,
        QueryOperator.InLastDays,
    ];

    public static readonly QueryOperator[] DueDate = [.. Date, QueryOperator.IsOverdue];

    public static readonly QueryOperator[] Collection =
    [
        QueryOperator.In,
        QueryOperator.NotIn,
        QueryOperator.IsEmpty,
        QueryOperator.IsNotEmpty,
    ];

    public static readonly QueryOperator[] Presence =
    [
        QueryOperator.IsEmpty,
        QueryOperator.IsNotEmpty,
    ];
}
