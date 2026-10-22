namespace Netptune.Core.Models.Pages;

public sealed class PublicPageScope
{
    public static readonly PublicPageScope Unrestricted = new(null);

    private readonly HashSet<int>? VisibleIds;

    private PublicPageScope(HashSet<int>? visibleIds)
    {
        VisibleIds = visibleIds;
    }

    public static PublicPageScope Restricted(HashSet<int> visibleIds) => new(visibleIds);

    public bool Allows(int pageId) => VisibleIds?.Contains(pageId) ?? true;
}
