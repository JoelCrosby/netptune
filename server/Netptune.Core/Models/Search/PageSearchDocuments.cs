using Netptune.Core.Models.Pages;

namespace Netptune.Core.Models.Search;

public static class PageSearchDocuments
{
    public const string IndexName = "pages";

    public static string DocumentId(int pageId)
    {
        return $"{SearchIndexEntityTypes.Page}_{pageId}";
    }

    public static List<PageSearchDocument> Build(
        IReadOnlyCollection<PageSearchSource> sources,
        IReadOnlyCollection<PageOutlineEntry> outline,
        string workspaceSlug)
    {
        var outlineById = outline.ToDictionary(entry => entry.Id);

        return sources.Select(source => ToDocument(source, outlineById, workspaceSlug)).ToList();
    }

    private static PageSearchDocument ToDocument(
        PageSearchSource source,
        Dictionary<int, PageOutlineEntry> outlineById,
        string workspaceSlug)
    {
        var path = BuildPath(source.ParentId, outlineById);

        return new PageSearchDocument
        {
            Id = DocumentId(source.Id),
            PageId = source.Id,
            Slug = source.Slug,
            Title = source.Title,
            Path = path,
            Body = PageSearchText.ToPlainText(source.Body),
            Kind = source.Kind.ToString(),
            Status = source.Status.ToString(),
            Tags = source.Tags,
            WorkspaceSlug = workspaceSlug,
            ProjectId = source.ProjectId,
            UpdatedAt = source.UpdatedAt,
        };
    }

    private static string? BuildPath(int? parentId, Dictionary<int, PageOutlineEntry> outlineById)
    {
        var titles = new List<string>();
        var visited = new HashSet<int>();
        var currentId = parentId;

        while (currentId is { } id)
        {
            var firstVisit = visited.Add(id);
            var isLive = outlineById.TryGetValue(id, out var entry);

            if (!firstVisit || !isLive)
            {
                break;
            }

            titles.Add(entry!.Title);
            currentId = entry.ParentId;
        }

        if (titles.Count == 0)
        {
            return null;
        }

        titles.Reverse();

        return string.Join(" / ", titles);
    }
}
