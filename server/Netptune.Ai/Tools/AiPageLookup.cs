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

    public static PageKind? ParseKind(string? kind)
    {
        return kind?.Trim().ToLowerInvariant() switch
        {
            null or "" or "document" => PageKind.Document,
            "folder" => PageKind.Folder,
            _ => null,
        };
    }

    // Reads parentId or parentRef. Neither means the top level; a parentRef must name a page proposed earlier.
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

    // Mentions of tasks proposed in the same change set, written [[task:ref:3|name]] until they exist.
    // Code is left alone, so a page documenting the syntax is not read as linking to anything.
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
