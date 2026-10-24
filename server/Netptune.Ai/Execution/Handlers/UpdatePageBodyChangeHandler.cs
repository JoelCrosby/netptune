using System.Text.Json;

using Mediator;

using Netptune.Ai.Tools;
using Netptune.Core.Authorization;
using Netptune.Core.Localization;
using Netptune.Core.Models.Ai;
using Netptune.Core.Requests.Pages;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.Pages.Commands;
using Netptune.Handlers.Tasks.Queries;

namespace Netptune.Ai.Execution.Handlers;

public sealed class UpdatePageBodyChangeHandler : IAiChangeHandler, IAiChangeUndoHandler
{
    public static string EditedSinceMessage => AppText.AiChangeErrorPageEditedAfterChangeProposedAskFresh;

    public static string UnresolvedTasksMessage => AppText.AiChangeErrorTasksPageShouldLinkNotCreatedPage;

    private readonly IMediator Mediator;

    public UpdatePageBodyChangeHandler(IMediator mediator)
    {
        Mediator = mediator;
    }

    public string ToolName => "propose_update_page_body";

    public IReadOnlySet<string> UndoPermissions { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        NetptunePermissions.Pages.Update,
    };

    public async Task<AiAppliedChangeResult> Apply(AiChangeApplyContext context, CancellationToken cancellationToken)
    {
        var change = context.Change;
        var payload = change.Payload.RootElement;
        var pageId = AiChangePayload.ReadInt(payload, "pageId") ?? change.EntityId;
        var revision = AiChangePayload.ReadInt(payload, "revision");

        if (!pageId.HasValue || !revision.HasValue)
        {
            return AiChangePayload.Failure(change, AppText.AiChangeErrorPageChangeRefersCouldNotResolved);
        }

        var proposedBody = AiChangePayload.ReadString(payload, "body");
        var body = await ResolveTaskHandles(context, proposedBody, cancellationToken);

        if (body.HasUnresolvedHandles)
        {
            return AiChangePayload.Failure(change, UnresolvedTasksMessage);
        }

        var request = new SavePageRequest
        {
            Title = AiChangePayload.ReadString(payload, "title"),
            Body = body.Text,
            Revision = revision.Value,
        };

        var response = await Mediator.Send(new SavePageCommand(pageId.Value, request), cancellationToken);

        if (response.IsConflict)
        {
            return AiChangePayload.Failure(change, EditedSinceMessage);
        }

        if (!response.IsSuccess)
        {
            return AiChangePayload.Failure(change, response.Message ?? AppText.AiChangeErrorPageCouldNotSaved);
        }

        return AiChangePayload.Applied(change, pageId);
    }

    public Task<JsonDocument?> Capture(AiChangeApplyContext context, CancellationToken cancellationToken)
    {
        var pageId = AiChangePayload.ReadInt(context.Change.Payload.RootElement, "pageId") ?? context.Change.EntityId;

        return AiPageUndo.Capture(Mediator, pageId, cancellationToken);
    }

    public Task<AiAppliedChangeResult> Revert(AiChangeUndoContext context, CancellationToken cancellationToken)
    {
        return AiPageUndo.RevertContent(Mediator, context.Change, cancellationToken);
    }

    private async Task<ResolvedPageBody> ResolveTaskHandles(
        AiChangeApplyContext context,
        string? body,
        CancellationToken cancellationToken)
    {
        var handles = body is null ? [] : AiPageLookup.ReadTaskHandles(body);

        if (body is null || handles.Count == 0)
        {
            return new ResolvedPageBody(body, false);
        }

        var taskIdsByHandle = handles
            .Where(handle => context.ResolvedRefs.ContainsKey(handle))
            .ToDictionary(handle => handle, handle => context.ResolvedRefs[handle], StringComparer.Ordinal);
        var taskIds = taskIdsByHandle.Values.Distinct().ToList();
        var systemIds = await Mediator.Send(new GetTaskSystemIdsQuery(taskIds), cancellationToken);
        var systemIdsByHandle = taskIdsByHandle
            .Where(pair => systemIds.ContainsKey(pair.Value))
            .ToDictionary(pair => pair.Key, pair => systemIds[pair.Value], StringComparer.Ordinal);
        var hasUnresolved = systemIdsByHandle.Count < handles.Count;
        var resolved = AiPageLookup.ReplaceTaskHandles(body, systemIdsByHandle);

        return new ResolvedPageBody(resolved, hasUnresolved);
    }

    private sealed record ResolvedPageBody(string? Text, bool HasUnresolvedHandles);
}
