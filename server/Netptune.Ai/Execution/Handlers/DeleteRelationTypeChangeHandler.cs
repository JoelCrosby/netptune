using Mediator;

using Netptune.Ai.Tools;
using Netptune.Core.Localization;
using Netptune.Core.Models.Ai;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.RelationTypes.Commands;

namespace Netptune.Ai.Execution.Handlers;

public sealed class DeleteRelationTypeChangeHandler : IAiChangeHandler
{
    private readonly IMediator Mediator;

    public DeleteRelationTypeChangeHandler(IMediator mediator)
    {
        Mediator = mediator;
    }

    public string ToolName => DeleteRelationTypeAction.Change;

    public async Task<AiAppliedChangeResult> Apply(
        AiChangeApplyContext context,
        CancellationToken cancellationToken)
    {
        var change = context.Change;
        var relationTypeId = AiChangePayload.ReadInt(change.Payload.RootElement, "relationTypeId") ?? change.EntityId;

        if (!relationTypeId.HasValue)
        {
            return AiChangePayload.Failure(change, AppText.AiChangeErrorRelationTypeChangeRefersCouldNotResolved);
        }

        var response = await Mediator.Send(new DeleteRelationTypeCommand(relationTypeId.Value), cancellationToken);

        if (!response.IsSuccess)
        {
            return AiChangePayload.Failure(
                change,
                response.Message ?? AppText.AiChangeErrorRelationTypeCouldNotDeleted);
        }

        return AiChangePayload.Applied(change, relationTypeId.Value);
    }
}
