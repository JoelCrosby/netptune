using Netptune.Core.Enums;

namespace Netptune.Core.Relations;

public static class RelationTypeRules
{
    public static bool IsSymmetric(RelationCategory category) => category is RelationCategory.Related;

    public static bool IsAcyclic(RelationCategory category) => category is RelationCategory.Hierarchy or RelationCategory.Dependency;

    public static bool HasSingleSource(RelationCategory category) => category is RelationCategory.Hierarchy;

    public static string ResolveInverseName(RelationCategory category, string name, string? inverseName)
    {
        if (IsSymmetric(category)) return name;

        return string.IsNullOrWhiteSpace(inverseName) ? name : inverseName.Trim();
    }
}
