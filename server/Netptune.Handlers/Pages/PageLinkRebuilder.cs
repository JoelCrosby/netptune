using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Models.Pages;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.Pages;

public static class PageLinkRebuilder
{
    public static async Task Rebuild(INetptuneUnitOfWork unitOfWork, PageContentUpdate update, CancellationToken cancellationToken)
    {
        var references = PageReferenceParser.Parse(update.Body);
        var taskReferences = references.Where(reference => reference.Kind == PageLinkTargetType.Task).ToList();
        var pageReferences = references.Where(reference => reference.Kind == PageLinkTargetType.Page).ToList();

        var taskLinks = await ResolveTaskLinks(unitOfWork, update, taskReferences, cancellationToken);
        var pageLinks = await ResolvePageLinks(unitOfWork, update, pageReferences, cancellationToken);
        var links = taskLinks.Concat(pageLinks).ToList();

        await unitOfWork.PageLinks.ReplaceExtracted(update.PageId, links, cancellationToken);
    }

    private static async Task<List<PageLink>> ResolveTaskLinks(
        INetptuneUnitOfWork unitOfWork,
        PageContentUpdate update,
        List<PageReference> references,
        CancellationToken cancellationToken)
    {
        if (references.Count == 0)
        {
            return [];
        }

        var systemIds = references.Select(reference => reference.Key).ToList();
        var tasks = await unitOfWork.Tasks.GetTaskViewModels(systemIds, update.WorkspaceKey, cancellationToken);
        var taskIdsBySystemId = tasks
            .GroupBy(task => task.SystemId.ToLowerInvariant())
            .ToDictionary(group => group.Key, group => group.First().Id);

        return references
            .Where(reference => taskIdsBySystemId.ContainsKey(reference.Key))
            .Select(reference => ToLink(update.PageId, PageLinkTargetType.Task, taskIdsBySystemId[reference.Key], reference))
            .ToList();
    }

    private static async Task<List<PageLink>> ResolvePageLinks(
        INetptuneUnitOfWork unitOfWork,
        PageContentUpdate update,
        List<PageReference> references,
        CancellationToken cancellationToken)
    {
        var numbered = references
            .Select(reference => (Id: ParsePageId(reference.Key), Reference: reference))
            .Where(pair => pair.Id > 0 && pair.Id != update.PageId)
            .ToList();

        if (numbered.Count == 0)
        {
            return [];
        }

        var referencedIds = numbered.Select(pair => pair.Id).ToList();
        var existingIds = await unitOfWork.Pages.GetExistingIds(referencedIds, update.WorkspaceId, cancellationToken);
        var existing = existingIds.ToHashSet();

        return numbered
            .Where(pair => existing.Contains(pair.Id))
            .Select(pair => ToLink(update.PageId, PageLinkTargetType.Page, pair.Id, pair.Reference))
            .ToList();
    }

    private static int ParsePageId(string key)
    {
        var isNumber = int.TryParse(key, out var id);

        return isNumber ? id : 0;
    }

    private static PageLink ToLink(int pageId, PageLinkTargetType targetType, int targetId, PageReference reference)
    {
        return new PageLink
        {
            PageId = pageId,
            TargetType = targetType,
            TargetId = targetId.ToString(),
            Snippet = reference.Snippet,
            IsExplicit = false,
        };
    }
}
