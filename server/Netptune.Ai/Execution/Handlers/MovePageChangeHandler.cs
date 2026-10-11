using System.Text.Json;

using Mediator;

using Netptune.Ai.Tools;
using Netptune.Core.Authorization;
using Netptune.Core.Entities;
using Netptune.Core.Models.Ai;
using Netptune.Core.Requests.Pages;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.Pages.Commands;

namespace Netptune.Ai.Execution.Handlers;

public sealed class MovePageChangeHandler : IAiChangeHandler, IAiChangeUndoHandler
{
    public const string MovedSinceMessage =
        "The page has been moved again since this change was applied, so it was left where it is now.";

    private readonly IMediator Mediator;

    public MovePageChangeHandler(IMediator mediator)
    {
        Mediator = mediator;
    }

    public string ToolName => "propose_move_page";

    public IReadOnlySet<string> UndoPermissions { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        NetptunePermissions.Pages.Update,
    };

    public async Task<AiAppliedChangeResult> Apply(AiChangeApplyContext context, CancellationToken cancellationToken)
    {
        var change = context.Change;
        var payload = change.Payload.RootElement;
        var pageId = AiChangePayload.ReadInt(payload, "pageId") ?? change.EntityId;

        if (!pageId.HasValue)
        {
            return AiChangePayload.Failure(change, "The page this change refers to could not be resolved.");
        }

        var parentId = AiChangePayload.ReadInt(payload, "parentId")
            ?? AiChangePayload.ResolveReference(context, "parentRef");
        var tree = await AiPageLookup.Tree(Mediator, cancellationToken);
        var siblingOrders = tree
            .Where(node => node.ParentId == parentId && node.Id != pageId.Value)
            .Select(node => node.SortOrder)
            .ToList();
        var lastOrder = siblingOrders.Count == 0 ? 0 : siblingOrders.Max();
        var request = new MovePageRequest { ParentId = parentId, SortOrder = lastOrder + 1 };
        var response = await Mediator.Send(new MovePageCommand(pageId.Value, request), cancellationToken);

        if (!response.IsSuccess)
        {
            return AiChangePayload.Failure(change, response.Message ?? "The page could not be moved.");
        }

        return AiChangePayload.Applied(change, pageId);
    }

    public Task<JsonDocument?> Capture(AiChangeApplyContext context, CancellationToken cancellationToken)
    {
        var pageId = AiChangePayload.ReadInt(context.Change.Payload.RootElement, "pageId") ?? context.Change.EntityId;

        return AiPageUndo.CapturePlacement(Mediator, pageId, cancellationToken);
    }

    public async Task<AiAppliedChangeResult> Revert(AiChangeUndoContext context, CancellationToken cancellationToken)
    {
        var change = context.Change;
        var snapshot = AiPageUndo.ReadSnapshot(change.UndoPayload);

        if (snapshot is null)
        {
            return AiChangeUndoResult.Failure(change, "There is nothing recorded to restore.");
        }

        var movedAgain = await WasMovedSinceApply(change, snapshot.PageId, cancellationToken);

        if (movedAgain)
        {
            return AiChangeUndoResult.Failure(change, MovedSinceMessage);
        }

        var request = new MovePageRequest { ParentId = snapshot.ParentId, SortOrder = snapshot.SortOrder };
        var response = await Mediator.Send(new MovePageCommand(snapshot.PageId, request), cancellationToken);

        if (!response.IsSuccess)
        {
            return AiChangeUndoResult.Failure(change, response.Message ?? "The page could not be moved back.");
        }

        return AiChangeUndoResult.Undone(change, snapshot.PageId);
    }

    // A parent given by handle is only known once applied, so only a move to a known parent can be checked.
    private async Task<bool> WasMovedSinceApply(AiProposedChange change, int pageId, CancellationToken cancellationToken)
    {
        var payload = change.Payload.RootElement;
        var hasKnownParent = AiChangePayload.ReadString(payload, "parentRef") is null;

        if (!hasKnownParent)
        {
            return false;
        }

        var appliedParentId = AiChangePayload.ReadInt(payload, "parentId");
        var tree = await AiPageLookup.Tree(Mediator, cancellationToken);
        var node = tree.FirstOrDefault(candidate => candidate.Id == pageId);

        return node is not null && node.ParentId != appliedParentId;
    }
}
