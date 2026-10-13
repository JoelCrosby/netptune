using System.Text.RegularExpressions;

using Mediator;

using Netptune.Core.Enums;
using Netptune.Core.Services.Ai;
using Netptune.Core.ViewModels.Pages;
using Netptune.Handlers.Pages.Queries;

namespace Netptune.Ai.Tools;

public sealed record AiPageParent(int? ParentId, string? ParentRef, string Display);

public sealed record AiPageParentLookup(AiPageParent? Parent, string? Error);

public static partial class AiPageLookup
{
    public const string EntityType = "page";

    public const string TopLevel = "Top level";

    public static async Task<List<PageTreeNodeViewModel>> Tree(IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetPageTreeQuery(), cancellationToken);
        var hasTree = result.IsSuccess && result.Payload is not null;

        return hasTree ? result.Payload! : [];
    }

    public static async Task<Dictionary<int, string>> Bodies(
        IMediator mediator,
        IReadOnlyCollection<int> ids,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetPageBodiesQuery(ids), cancellationToken);
        var hasBodies = result.IsSuccess && result.Payload is not null;
        var bodies = hasBodies ? result.Payload! : [];

        return bodies.ToDictionary(page => page.Id, page => page.Body);
    }

    public static List<PageTreeNodeViewModel> InTreeOrder(IReadOnlyList<PageTreeNodeViewModel> tree, int? rootId = null)
    {
        var liveIds = tree.Select(node => node.Id).ToHashSet();
        var childrenByParent = tree
            .Where(node => node.ParentId is { } parentId && liveIds.Contains(parentId))
            .ToLookup(node => node.ParentId!.Value);
        var topLevel = tree.Where(node => node.ParentId is not { } parentId || !liveIds.Contains(parentId));
        var starts = rootId is { } id ? childrenByParent[id] : topLevel;
        var ordered = new List<PageTreeNodeViewModel>();
        var visited = new HashSet<int>();
        var pending = new Stack<PageTreeNodeViewModel>(starts.OrderByDescending(node => node.SortOrder));

        while (pending.TryPop(out var node))
        {
            if (!visited.Add(node.Id))
            {
                continue;
            }

            ordered.Add(node);

            var children = childrenByParent[node.Id].OrderByDescending(child => child.SortOrder);

            foreach (var child in children)
            {
                pending.Push(child);
            }
        }

        return ordered;
    }

    public static Dictionary<int, string?> Paths(IReadOnlyList<PageTreeNodeViewModel> tree)
    {
        var nodesById = tree.ToDictionary(node => node.Id);

        return tree.ToDictionary(node => node.Id, node => BuildPath(node.ParentId, nodesById));
    }

    private static string? BuildPath(int? parentId, Dictionary<int, PageTreeNodeViewModel> nodesById)
    {
        var titles = new List<string>();
        var visited = new HashSet<int>();
        var currentId = parentId;

        while (currentId is { } id && visited.Add(id) && nodesById.TryGetValue(id, out var parent))
        {
            titles.Add(parent.Title);
            currentId = parent.ParentId;
        }

        if (titles.Count == 0)
        {
            return null;
        }

        titles.Reverse();

        return string.Join(" / ", titles);
    }

    public static PageKind? ParseKind(string? kind)
    {
        return kind?.Trim().ToLowerInvariant() switch
        {
            null or "" or "document" => PageKind.Document,
            "folder" => PageKind.Folder,
            _ => null,
        };
    }

    public static async Task<PageViewModel?> Read(IMediator mediator, int pageId, CancellationToken cancellationToken)
    {
        var tree = await Tree(mediator, cancellationToken);
        var node = tree.FirstOrDefault(candidate => candidate.Id == pageId);

        if (node is null)
        {
            return null;
        }

        var result = await mediator.Send(new GetPageQuery(node.Slug), cancellationToken);

        return result.IsSuccess ? result.Payload : null;
    }

    public static async Task<List<ArchivedPageViewModel>> Archived(IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetArchivedPagesQuery(), cancellationToken);
        var hasPages = result.IsSuccess && result.Payload is not null;

        return hasPages ? result.Payload! : [];
    }

    public static string NotArchivedMessage(int pageId)
    {
        return $"Page {pageId} is not in the archive. Find archived pages with list_archived_pages.";
    }

    public static PageStatus? ParseStatus(string? status)
    {
        return status?.Trim().ToLowerInvariant() switch
        {
            "draft" => PageStatus.Draft,
            "published" => PageStatus.Published,
            _ => null,
        };
    }

    public static string DescribeStatus(PageStatus status)
    {
        return status.ToString().ToLowerInvariant();
    }

    public static AiPageParentLookup ReadParent(
        IReadOnlyList<PageTreeNodeViewModel> tree,
        IAiChangeSetBuilder changeSet,
        int? parentId,
        string? parentRef)
    {
        if (parentRef is not null)
        {
            var pending = AiPendingReference.Find(changeSet, parentRef, EntityType);

            if (pending is null)
            {
                return new AiPageParentLookup(null, AiPendingReference.Missing(parentRef, EntityType));
            }

            var pendingName = AiPendingReference.ProposedName(pending);

            return new AiPageParentLookup(new AiPageParent(null, parentRef, pendingName), null);
        }

        if (!parentId.HasValue)
        {
            return new AiPageParentLookup(new AiPageParent(null, null, TopLevel), null);
        }

        var parent = tree.FirstOrDefault(node => node.Id == parentId.Value);

        if (parent is null)
        {
            return new AiPageParentLookup(null, $"Page {parentId} is not in this workspace.");
        }

        return new AiPageParentLookup(new AiPageParent(parent.Id, null, parent.Title), null);
    }

    public static List<string> ReadTaskHandles(string body)
    {
        var code = CodeSpans(body);

        return TaskHandlePattern()
            .Matches(body)
            .Where(match => !IsInCode(code, match.Index))
            .Select(match => match.Groups["handle"].Value)
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    public static string ReplaceTaskHandles(string body, IReadOnlyDictionary<string, string> systemIdsByHandle)
    {
        var code = CodeSpans(body);

        return TaskHandlePattern().Replace(body, match =>
        {
            var handle = match.Groups["handle"].Value;
            var isKnown = systemIdsByHandle.TryGetValue(handle, out var systemId);
            var isInCode = IsInCode(code, match.Index);

            return isKnown && !isInCode ? $"[[task:{systemId}" : match.Value;
        });
    }

    private static List<(int Start, int End)> CodeSpans(string body)
    {
        var fences = FencedCodePattern().Matches(body);
        var inline = InlineCodePattern().Matches(body);

        return fences
            .Concat(inline)
            .Select(match => (match.Index, match.Index + match.Length))
            .ToList();
    }

    private static bool IsInCode(List<(int Start, int End)> code, int index)
    {
        return code.Any(span => index >= span.Start && index < span.End);
    }

    [GeneratedRegex(@"\[\[task:(?<handle>ref:\d+)(?=[|\]])")]
    private static partial Regex TaskHandlePattern();

    [GeneratedRegex(@"^[ \t]*(```|~~~).*?^[ \t]*\1", RegexOptions.Multiline | RegexOptions.Singleline)]
    private static partial Regex FencedCodePattern();

    [GeneratedRegex(@"`[^`\n]*`")]
    private static partial Regex InlineCodePattern();

    public static bool HasLiveChildren(IReadOnlyList<PageTreeNodeViewModel> tree, int pageId)
    {
        return tree.Any(node => node.ParentId == pageId);
    }

    public static int CountDescendants(IReadOnlyList<PageTreeNodeViewModel> tree, int pageId)
    {
        var childIdsByParent = tree
            .Where(node => node.ParentId.HasValue)
            .ToLookup(node => node.ParentId!.Value, node => node.Id);
        var visited = new HashSet<int> { pageId };
        var pending = new Stack<int>([pageId]);

        while (pending.TryPop(out var parentId))
        {
            foreach (var childId in childIdsByParent[parentId])
            {
                if (visited.Add(childId))
                {
                    pending.Push(childId);
                }
            }
        }

        return visited.Count - 1;
    }

    public static bool IsInside(IReadOnlyList<PageTreeNodeViewModel> tree, int candidateId, int ancestorId)
    {
        var parentsById = tree.ToDictionary(node => node.Id, node => node.ParentId);
        var visited = new HashSet<int>();
        int? currentId = candidateId;

        while (currentId is { } id && visited.Add(id))
        {
            if (id == ancestorId)
            {
                return true;
            }

            currentId = parentsById.GetValueOrDefault(id);
        }

        return false;
    }
}
