using System.Text.Json;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Models.Ai;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.Pages.Commands;

namespace Netptune.Ai.Execution.Handlers;

public sealed class ArchivePageChangeHandler : IAiChangeHandler, IAiChangeUndoHandler
{
    public const string NotYourPageMessage =
        "Only the person who created this page, or someone allowed to manage every page, can archive, restore or delete it.";

    private readonly IMediator Mediator;

    public ArchivePageChangeHandler(IMediator mediator)
    {
        Mediator = mediator;
    }

    public string ToolName => "propose_archive_page";

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
            return AiChangePayload.Failure(change, "The page this change refers to could not be resolved.");
        }

        var response = await Mediator.Send(new ArchivePageCommand(pageId.Value), cancellationToken);

        if (!response.IsSuccess)
        {
            var message = response.IsForbidden ? NotYourPageMessage : response.Message ?? "The page could not be archived.";

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
            return AiChangeUndoResult.Failure(change, "The archived page could not be resolved.");
        }

        var response = await Mediator.Send(new RestorePageCommand(pageId.Value), cancellationToken);

        if (!response.IsSuccess)
        {
            var message = response.IsForbidden ? NotYourPageMessage : response.Message ?? "The page could not be restored.";

            return AiChangeUndoResult.Failure(change, message);
        }

        return AiChangeUndoResult.Undone(change, pageId);
    }
}
