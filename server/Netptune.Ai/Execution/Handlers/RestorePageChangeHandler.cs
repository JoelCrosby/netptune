using System.Text.Json;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Localization;
using Netptune.Core.Models.Ai;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.Pages.Commands;

namespace Netptune.Ai.Execution.Handlers;

public sealed class RestorePageChangeHandler : IAiChangeHandler, IAiChangeUndoHandler
{
    private readonly IMediator Mediator;

    public RestorePageChangeHandler(IMediator mediator)
    {
        Mediator = mediator;
    }

    public string ToolName => "propose_restore_page";

    public IReadOnlySet<string> UndoPermissions { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        NetptunePermissions.Pages.Delete,
    };

    public async Task<AiAppliedChangeResult> Apply(AiChangeApplyContext context, CancellationToken cancellationToken)
    {
        var change = context.Change;
        var pageId = AiChangePayload.ReadInt(change.Payload.RootElement, "pageId") ?? change.EntityId;

        if (!pageId.HasValue)
        {
            return AiChangePayload.Failure(change, AppText.AiChangeErrorPageChangeRefersCouldNotResolved);
        }

        var response = await Mediator.Send(new RestorePageCommand(pageId.Value), cancellationToken);

        if (!response.IsSuccess)
        {
            var message = response.IsForbidden
                ? ArchivePageChangeHandler.NotYourPageMessage
                : response.Message ?? AppText.AiChangeErrorPageCouldNotRestored;

            return AiChangePayload.Failure(change, message);
        }

        return AiChangePayload.Applied(change, pageId);
    }

    public Task<JsonDocument?> Capture(AiChangeApplyContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult<JsonDocument?>(null);
    }

    public async Task<AiAppliedChangeResult> Revert(AiChangeUndoContext context, CancellationToken cancellationToken)
    {
        var change = context.Change;
        var pageId = change.AppliedEntityId ?? change.EntityId;

        if (!pageId.HasValue)
        {
            return AiChangeUndoResult.Failure(change, AppText.AiChangeErrorRestoredPageCouldNotResolved);
        }

        var response = await Mediator.Send(new ArchivePageCommand(pageId.Value), cancellationToken);

        if (!response.IsSuccess)
        {
            var message = response.IsForbidden
                ? ArchivePageChangeHandler.NotYourPageMessage
                : response.Message ?? AppText.AiChangeErrorPageCouldNotArchivedAgain;

            return AiChangeUndoResult.Failure(change, message);
        }

        return AiChangeUndoResult.Undone(change, pageId);
    }
}
