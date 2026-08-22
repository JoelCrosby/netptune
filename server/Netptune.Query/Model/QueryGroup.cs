namespace Netptune.Query.Model;

public sealed record QueryGroup
{
    public QueryGroupOperator Operator { get; init; }

    public List<QueryCondition> Conditions { get; init; } = [];

    public List<QueryGroup> Groups { get; init; } = [];

    public int CountConditions()
    {
        var nestedCount = Groups.Sum(group => group.CountConditions());

        return Conditions.Count + nestedCount;
    }

    public bool IsEmpty()
    {
        var hasNoConditions = Conditions.Count == 0;
        var hasNoNestedGroups = Groups.Count == 0;

        return hasNoConditions && hasNoNestedGroups;
    }
}
