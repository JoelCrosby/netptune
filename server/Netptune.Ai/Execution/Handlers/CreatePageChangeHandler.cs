using System.Text.Json;

using Mediator;

using Netptune.Ai.Tools;
using Netptune.Core.Authorization;
using Netptune.Core.Models.Ai;
using Netptune.Core.Requests.Pages;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.Pages.Commands;

namespace Netptune.Ai.Execution.Handlers;

public sealed class CreatePageChangeHandler : IAiChangeHandler, IAiChangeUndoHandler
{
    public const string HoldsOtherPagesMessage =
        "Pages have been added inside it since, so it was left in place. Archive it from the page tree if it should go.";

    private readonly IMediator Mediator;

    public CreatePageChangeHandler(IMediator mediator)
    {
        Mediator = mediator;
    }

    public string ToolName => "propose_create_page";

    public IReadOnlySet<string> UndoPermissions { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        NetptunePermissions.Pages.Delete,
    };

    public async Task<AiAppliedChangeResult> Apply(AiChangeApplyContext context, CancellationToken cancellationToken)
    {
        var change = context.Change;
        var payload = change.Payload.RootElement;
        var title = AiChangePayload.ReadString(payload, "title");
        var kind = AiPageLookup.ParseKind(AiChangePayload.ReadString(payload, "kind"));
        var body = AiChangePayload.ReadString(payload, "body");
        var parentId = AiChangePayload.ReadInt(payload, "parentId")
            ?? AiChangePayload.ResolveReference(context, "parentRef");
        var hasTitle = !string.IsNullOrWhiteSpace(title);
        var isComplete = hasTitle && kind.HasValue;

        if (!isComplete)
        {
            return AiChangePayload.Failure(change, "The page details are missing from this change.");
        }

        var request = new CreatePageRequest { Kind = kind.Value, ParentId = parentId, Title = title, Body = body };
        var created = await Mediator.Send(new CreatePageCommand(request), cancellationToken);
        var page = created.Payload;
        var isCreated = created.IsSuccess && page is not null;

        if (!isCreated)
        {
            return AiChangePayload.Failure(change, created.Message ?? "The page could not be created.");
        }

        return AiChangePayload.Applied(change, page!.Id);
    }

    // Nothing existed before, so the undo only needs the created page.
    public Task<JsonDocument?> Capture(AiChangeApplyContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult<JsonDocument?>(null);
    }

    public async Task<AiAppliedChangeResult> Revert(AiChangeUndoContext context, CancellationToken cancellationToken)
    {
        var change = context.Change;
        var pageId = change.AppliedEntityId;

        if (!pageId.HasValue)
        {
            return AiChangeUndoResult.Failure(change, "The created page could not be resolved.");
        }

        // Pages this change set created inside are undone first, so anything still inside was added since,
        // and archiving the page would take it along unseen.
        var tree = await AiPageLookup.Tree(Mediator, cancellationToken);
        var holdsOtherPages = AiPageLookup.HasLiveChildren(tree, pageId.Value);

        if (holdsOtherPages)
        {
            return AiChangeUndoResult.Failure(change, HoldsOtherPagesMessage);
        }

        var response = await Mediator.Send(new ArchivePageCommand(pageId.Value), cancellationToken);

        if (!response.IsSuccess)
        {
            return AiChangeUndoResult.Failure(change, response.Message ?? "The created page could not be archived.");
        }

        return AiChangeUndoResult.Undone(change, pageId);
    }
}
