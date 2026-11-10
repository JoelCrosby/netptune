using Netptune.Core.Enums;

namespace Netptune.Import;

public sealed record ImportLinks
{
    public required int TaskId { get; init; }

    public required IReadOnlyList<ImportLink> Links { get; init; }

    public static IReadOnlyList<ImportLink> From(ResolvedTaskRow row)
    {
        var links = new List<ImportLink>();

        if (row.ParentRef is not null)
        {
            links.Add(new ImportLink(RelationCategory.Hierarchy, row.ParentRef, TaskIsSource: false));
        }

        links.AddRange(ToLinks(row.BlockedByRefs, RelationCategory.Dependency, taskIsSource: false));
        links.AddRange(ToLinks(row.BlocksRefs, RelationCategory.Dependency, taskIsSource: true));
        links.AddRange(ToLinks(row.RelatedToRefs, RelationCategory.Related, taskIsSource: true));
        links.AddRange(ToLinks(row.DuplicatesRefs, RelationCategory.Duplicate, taskIsSource: true));

        return links;
    }

    private static IEnumerable<ImportLink> ToLinks(
        IEnumerable<string> references,
        RelationCategory category,
        bool taskIsSource)
    {
        return references.Select(reference => new ImportLink(category, reference, taskIsSource));
    }
}

public sealed record ImportLink(RelationCategory Category, string Reference, bool TaskIsSource);
