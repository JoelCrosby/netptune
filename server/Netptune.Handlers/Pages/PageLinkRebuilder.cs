using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Models.Pages;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.Pages;

public sealed record PageLinkSource(int PageId, string? Body);

public sealed record PageLinkScope(int WorkspaceId, string WorkspaceKey);

public static class PageLinkRebuilder
{
    public static async Task Rebuild(INetptuneUnitOfWork unitOfWork, PageContentUpdate update, CancellationToken cancellationToken)
    {
        var source = new PageLinkSource(update.PageId, update.Body);
        var scope = new PageLinkScope(update.WorkspaceId, update.WorkspaceKey);
        var links = await Extract(unitOfWork, [source], scope, cancellationToken);

        await unitOfWork.PageLinks.ReplaceExtracted(update.PageId, links, cancellationToken);
    }

    public static async Task BuildForNewPages(
        INetptuneUnitOfWork unitOfWork,
        IReadOnlyList<PageLinkSource> sources,
        PageLinkScope scope,
        CancellationToken cancellationToken)
    {
        var links = await Extract(unitOfWork, sources, scope, cancellationToken);

        if (links.Count == 0)
        {
            return;
        }

        await unitOfWork.PageLinks.AddRangeAsync(links, cancellationToken);
    }

    private static async Task<List<PageLink>> Extract(
        INetptuneUnitOfWork unitOfWork,
        IReadOnlyList<PageLinkSource> sources,
        PageLinkScope scope,
        CancellationToken cancellationToken)
    {
        var references = sources
            .SelectMany(source => PageReferenceParser.Parse(source.Body).Select(reference => new SourcedReference(source.PageId, reference)))
            .ToList();
        var taskReferences = references.Where(sourced => sourced.Reference.Kind == PageLinkTargetType.Task).ToList();
        var pageReferences = references.Where(sourced => sourced.Reference.Kind == PageLinkTargetType.Page).ToList();

        var taskLinks = await ResolveTaskLinks(unitOfWork, scope, taskReferences, cancellationToken);
        var pageLinks = await ResolvePageLinks(unitOfWork, scope, pageReferences, cancellationToken);
        var links = taskLinks.Concat(pageLinks).ToList();

        return links;
    }

    private static async Task<List<PageLink>> ResolveTaskLinks(
        INetptuneUnitOfWork unitOfWork,
        PageLinkScope scope,
        List<SourcedReference> references,
        CancellationToken cancellationToken)
    {
        if (references.Count == 0)
        {
            return [];
        }

        var systemIds = references.Select(sourced => sourced.Reference.Key).Distinct().ToList();
        var tasks = await unitOfWork.Tasks.GetTaskViewModels(systemIds, scope.WorkspaceKey, cancellationToken);
        var taskIdsBySystemId = tasks
            .GroupBy(task => task.SystemId.ToLowerInvariant())
            .ToDictionary(group => group.Key, group => group.First().Id);

        return references
            .Where(sourced => taskIdsBySystemId.ContainsKey(sourced.Reference.Key))
            .Select(sourced => ToLink(sourced, PageLinkTargetType.Task, taskIdsBySystemId[sourced.Reference.Key]))
            .ToList();
    }

    private static async Task<List<PageLink>> ResolvePageLinks(
        INetptuneUnitOfWork unitOfWork,
        PageLinkScope scope,
        List<SourcedReference> references,
        CancellationToken cancellationToken)
    {
        var numbered = references
            .Select(sourced => (Id: ParsePageId(sourced.Reference.Key), Sourced: sourced))
            .Where(pair => pair.Id > 0 && pair.Id != pair.Sourced.PageId)
            .ToList();

        if (numbered.Count == 0)
        {
            return [];
        }

        var referencedIds = numbered.Select(pair => pair.Id).Distinct().ToList();
        var existingIds = await unitOfWork.Pages.GetExistingIds(referencedIds, scope.WorkspaceId, cancellationToken);
        var existing = existingIds.ToHashSet();

        return numbered
            .Where(pair => existing.Contains(pair.Id))
            .Select(pair => ToLink(pair.Sourced, PageLinkTargetType.Page, pair.Id))
            .ToList();
    }

    private static int ParsePageId(string key)
    {
        var isNumber = int.TryParse(key, out var id);

        return isNumber ? id : 0;
    }

    private static PageLink ToLink(SourcedReference sourced, PageLinkTargetType targetType, int targetId)
    {
        return new PageLink
        {
            PageId = sourced.PageId,
            TargetType = targetType,
            TargetId = targetId.ToString(),
            Snippet = sourced.Reference.Snippet,
            IsExplicit = false,
        };
    }

    private sealed record SourcedReference(int PageId, PageReference Reference);
}
