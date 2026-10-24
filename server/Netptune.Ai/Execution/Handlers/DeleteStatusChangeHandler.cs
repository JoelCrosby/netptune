using Mediator;

using Netptune.Ai.Tools;
using Netptune.Core.Localization;
using Netptune.Core.Models.Ai;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.Statuses.Commands;

namespace Netptune.Ai.Execution.Handlers;

public sealed class DeleteStatusChangeHandler : IAiChangeHandler
{
    private readonly IMediator Mediator;

    public DeleteStatusChangeHandler(IMediator mediator)
    {
        Mediator = mediator;
    }

    public string ToolName => DeleteStatusAction.Change;

    public async Task<AiAppliedChangeResult> Apply(
        AiChangeApplyContext context,
        CancellationToken cancellationToken)
    {
        var change = context.Change;
        var statusId = AiChangePayload.ReadInt(change.Payload.RootElement, "statusId") ?? change.EntityId;

        if (!statusId.HasValue)
        {
            return AiChangePayload.Failure(change, AppText.AiChangeErrorStatusChangeRefersCouldNotResolved);
        }

        var response = await Mediator.Send(new DeleteStatusCommand(statusId.Value), cancellationToken);

        if (!response.IsSuccess)
        {
            return AiChangePayload.Failure(change, response.Message ?? AppText.AiChangeErrorStatusCouldNotDeleted);
        }

        return AiChangePayload.Applied(change, statusId.Value);
    }
}
