namespace Netptune.Query.Model;

public sealed record QueryCondition
{
    public required string Field { get; init; }

    public QueryOperator Operator { get; init; }

    public List<string> Values { get; init; } = [];
}
