using Netptune.Core.Enums;

namespace Netptune.Core.Relations;

public static class RelationTypeRules
{
    public static bool IsSymmetric(RelationCategory category) => category is RelationCategory.Related;

    public static bool IsAcyclic(RelationCategory category) => category is RelationCategory.Hierarchy or RelationCategory.Dependency;

    public static bool HasSingleSource(RelationCategory category) => category is RelationCategory.Hierarchy;

    // Symmetric relations have no meaningful direction, so the pair is stored in a canonical order.
    // Without this, "A relates to B" and "B relates to A" would be two different rows and the
    // uniqueness index would not catch the duplicate.
    public static (int Source, int Target) Orient(RelationCategory category, int sourceTaskId, int targetTaskId)
    {
        if (!IsSymmetric(category))
        {
            return (sourceTaskId, targetTaskId);
        }

        return sourceTaskId < targetTaskId
            ? (sourceTaskId, targetTaskId)
            : (targetTaskId, sourceTaskId);
    }

    public static string ResolveInverseName(RelationCategory category, string name, string? inverseName)
    {
        if (IsSymmetric(category)) return name;

        return string.IsNullOrWhiteSpace(inverseName) ? name : inverseName.Trim();
    }
}
