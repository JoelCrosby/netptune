using Mediator;

using Netptune.Core.Models.Ai;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.Pages.Commands;

namespace Netptune.Ai.Execution.Handlers;

public sealed class DeletePageChangeHandler : IAiChangeHandler
{
    private readonly IMediator Mediator;

    public DeletePageChangeHandler(IMediator mediator)
    {
        Mediator = mediator;
    }

    public string ToolName => "propose_delete_page";

    public async Task<AiAppliedChangeResult> Apply(AiChangeApplyContext context, CancellationToken cancellationToken)
    {
        var change = context.Change;
        var pageId = AiChangePayload.ReadInt(change.Payload.RootElement, "pageId") ?? change.EntityId;

        if (!pageId.HasValue)
        {
            return AiChangePayload.Failure(change, "The page this change refers to could not be resolved.");
        }

        var response = await Mediator.Send(new DeleteArchivedPageCommand(pageId.Value), cancellationToken);

        if (!response.IsSuccess)
        {
            var message = response.IsForbidden
                ? ArchivePageChangeHandler.NotYourPageMessage
                : response.Message ?? "The page could not be deleted.";

            return AiChangePayload.Failure(change, message);
        }

        return AiChangePayload.Applied(change, pageId);
    }
}
