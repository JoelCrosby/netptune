using System.Text.Json;

using Mediator;

using Netptune.Ai.Tools;
using Netptune.Core.ViewModels.Pages;
using Netptune.Handlers.Pages.Queries;

namespace Netptune.Ai.Execution.Handlers;

public sealed record AiPageSnapshot
{
    public int PageId { get; init; }

    public string Title { get; init; } = string.Empty;

    public string? Body { get; init; }

    public int Revision { get; init; }

    public int? ParentId { get; init; }

    public double SortOrder { get; init; }
}

public static class AiPageUndo
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static async Task<PageViewModel?> Read(IMediator mediator, int pageId, CancellationToken cancellationToken)
    {
        var (_, page) = await ReadWithPlacement(mediator, pageId, cancellationToken);

        return page;
    }

    public static async Task<JsonDocument?> Capture(IMediator mediator, int? pageId, CancellationToken cancellationToken)
    {
        if (!pageId.HasValue)
        {
            return null;
        }

        var (node, page) = await ReadWithPlacement(mediator, pageId.Value, cancellationToken);

        if (node is null || page is null)
        {
            return null;
        }

        var snapshot = new AiPageSnapshot
        {
            PageId = page.Id,
            Title = page.Title,
            Body = page.Body,
            Revision = page.Revision,
            ParentId = node.ParentId,
            SortOrder = node.SortOrder,
        };

        return JsonSerializer.SerializeToDocument(snapshot, SerializerOptions);
    }

    public static async Task<JsonDocument?> CapturePlacement(IMediator mediator, int? pageId, CancellationToken cancellationToken)
    {
        if (!pageId.HasValue)
        {
            return null;
        }

        var tree = await AiPageLookup.Tree(mediator, cancellationToken);
        var node = tree.FirstOrDefault(candidate => candidate.Id == pageId.Value);

        if (node is null)
        {
            return null;
        }

        var snapshot = new AiPageSnapshot
        {
            PageId = node.Id,
            Title = node.Title,
            ParentId = node.ParentId,
            SortOrder = node.SortOrder,
        };

        return JsonSerializer.SerializeToDocument(snapshot, SerializerOptions);
    }

    public static AiPageSnapshot? ReadSnapshot(JsonDocument? document)
    {
        if (document is null)
        {
            return null;
        }

        return document.RootElement.Deserialize<AiPageSnapshot>(SerializerOptions);
    }

    private static async Task<(PageTreeNodeViewModel? Node, PageViewModel? Page)> ReadWithPlacement(
        IMediator mediator,
        int pageId,
        CancellationToken cancellationToken)
    {
        var tree = await AiPageLookup.Tree(mediator, cancellationToken);
        var node = tree.FirstOrDefault(candidate => candidate.Id == pageId);

        if (node is null)
        {
            return (null, null);
        }

        var result = await mediator.Send(new GetPageQuery(node.Slug), cancellationToken);
        var page = result.IsSuccess ? result.Payload : null;

        return (node, page);
    }
}
