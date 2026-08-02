using Mediator;

using Netptune.Core.Models.Ai;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.Relations.Commands;

namespace Netptune.Ai.Execution.Handlers;

public sealed class UnlinkTasksChangeHandler : IAiChangeHandler
{
    private readonly IMediator Mediator;

    public UnlinkTasksChangeHandler(IMediator mediator)
    {
        Mediator = mediator;
    }

    public string ToolName => "propose_unlink_tasks";

    public async Task<AiAppliedChangeResult> Apply(
        AiChangeApplyContext context,
        CancellationToken cancellationToken)
    {
        var change = context.Change;
        var relationId = AiChangePayload.ReadInt(change.Payload.RootElement, "relationId");

        if (!relationId.HasValue)
        {
            return AiChangePayload.Failure(change, "The link this change refers to could not be resolved.");
        }

        var response = await Mediator.Send(new DeleteTaskRelationCommand(relationId.Value), cancellationToken);

        if (!response.IsSuccess)
        {
            return AiChangePayload.Failure(change, response.Message ?? "The link could not be removed.");
        }

        return AiChangePayload.Applied(change, change.EntityId);
    }
}
