using System.Text.Json;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Models.Ai;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.Pages.Commands;

namespace Netptune.Ai.Execution.Handlers;

public sealed class RestorePageVersionChangeHandler : IAiChangeHandler, IAiChangeUndoHandler
{
    private readonly IMediator Mediator;

    public RestorePageVersionChangeHandler(IMediator mediator)
    {
        Mediator = mediator;
    }

    public string ToolName => "propose_restore_page_version";

    public IReadOnlySet<string> UndoPermissions { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        NetptunePermissions.Pages.Update,
    };

    public async Task<AiAppliedChangeResult> Apply(AiChangeApplyContext context, CancellationToken cancellationToken)
    {
        var change = context.Change;
        var payload = change.Payload.RootElement;
        var pageId = AiChangePayload.ReadInt(payload, "pageId") ?? change.EntityId;
        var versionId = AiChangePayload.ReadInt(payload, "versionId");
        var proposedRevision = AiChangePayload.ReadInt(payload, "revision");

        if (!pageId.HasValue || !versionId.HasValue || !proposedRevision.HasValue)
        {
            return AiChangePayload.Failure(change, "The page version this change refers to could not be resolved.");
        }

        var command = new RestorePageVersionCommand(pageId.Value, versionId.Value, proposedRevision.Value);
        var response = await Mediator.Send(command, cancellationToken);

        if (response.IsConflict)
        {
            return AiChangePayload.Failure(change, UpdatePageBodyChangeHandler.EditedSinceMessage);
        }

        if (!response.IsSuccess)
        {
            return AiChangePayload.Failure(change, response.Message ?? "The page version could not be restored.");
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
}
