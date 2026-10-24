using System.Text.Json;

using Mediator;

using Netptune.Ai.Tools;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Localization;
using Netptune.Core.Models.Ai;
using Netptune.Core.Requests.Pages;
using Netptune.Core.ViewModels.Pages;
using Netptune.Handlers.Pages.Commands;
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

    public PageStatus? Status { get; init; }

    public List<int>? TagIds { get; init; }
}

public static class AiPageUndo
{
    public static string EditedSinceApplyMessage => AppText.AiChangeErrorPageEditedAfterChangeAppliedRestoreEarlier;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

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

    public static async Task<JsonDocument?> CaptureDetails(IMediator mediator, int? pageId, CancellationToken cancellationToken)
    {
        if (!pageId.HasValue)
        {
            return null;
        }

        var page = await AiPageLookup.Read(mediator, pageId.Value, cancellationToken);

        if (page is null)
        {
            return null;
        }

        var snapshot = new AiPageSnapshot
        {
            PageId = page.Id,
            Status = page.Status,
            TagIds = page.Tags.Select(tag => tag.Id).ToList(),
        };

        return JsonSerializer.SerializeToDocument(snapshot, SerializerOptions);
    }

    // The apply wrote exactly one revision; anything past it is someone else's edit, which an undo would lose.
    public static async Task<AiAppliedChangeResult> RevertContent(
        IMediator mediator,
        AiProposedChange change,
        CancellationToken cancellationToken)
    {
        var snapshot = ReadSnapshot(change.UndoPayload);

        if (snapshot is null)
        {
            return AiChangeUndoResult.Failure(change, AppText.AiChangeErrorNothingRecordedRestore);
        }

        var current = await AiPageLookup.Read(mediator, snapshot.PageId, cancellationToken);

        if (current is null)
        {
            return AiChangeUndoResult.Failure(change, AppText.AiChangeErrorPageNoLongerWorkspace);
        }

        var editedSinceApply = current.Revision != snapshot.Revision + 1;

        if (editedSinceApply)
        {
            return AiChangeUndoResult.Failure(change, EditedSinceApplyMessage);
        }

        var request = new SavePageRequest { Title = snapshot.Title, Body = snapshot.Body, Revision = current.Revision };
        var response = await mediator.Send(new SavePageCommand(snapshot.PageId, request), cancellationToken);

        if (!response.IsSuccess)
        {
            return AiChangeUndoResult.Failure(change, response.Message ?? AppText.AiChangeErrorPageCouldNotRestored);
        }

        return AiChangeUndoResult.Undone(change, snapshot.PageId);
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
