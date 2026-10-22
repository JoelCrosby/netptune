using Netptune.Core.Enums;

namespace Netptune.Core.Models.Pages;

// A folder's own status is ignored: there is no way to publish one in the app, so it is shown whenever
// a visible document sits inside it.
public static class PublicPageVisibility
{
    public static HashSet<int> VisibleIds(IReadOnlyCollection<PageVisibilityNode> nodes)
    {
        var byId = nodes.ToDictionary(node => node.Id);
        var visible = new HashSet<int>();

        foreach (var node in nodes)
        {
            var isPublishedDocument = IsPublishedDocument(node);

            if (!isPublishedDocument)
            {
                continue;
            }

            var ancestors = AncestorsOf(node, byId);
            var hasDraftAncestor = ancestors.Any(IsDraftDocument);

            if (hasDraftAncestor)
            {
                continue;
            }

            visible.Add(node.Id);
            visible.UnionWith(ancestors.Select(ancestor => ancestor.Id));
        }

        return visible;
    }

    private static List<PageVisibilityNode> AncestorsOf(PageVisibilityNode node, Dictionary<int, PageVisibilityNode> byId)
    {
        var ancestors = new List<PageVisibilityNode>();
        var seen = new HashSet<int> { node.Id };
        var parentId = node.ParentId;

        while (parentId is { } id && seen.Add(id) && byId.TryGetValue(id, out var parent))
        {
            ancestors.Add(parent);
            parentId = parent.ParentId;
        }

        return ancestors;
    }

    private static bool IsPublishedDocument(PageVisibilityNode node)
    {
        return node.Kind == PageKind.Document && node.Status == PageStatus.Published;
    }

    private static bool IsDraftDocument(PageVisibilityNode node)
    {
        return node.Kind == PageKind.Document && node.Status != PageStatus.Published;
    }
}
