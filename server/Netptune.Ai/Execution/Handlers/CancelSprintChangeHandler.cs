using Mediator;

using Netptune.Core.Enums;
using Netptune.Core.Models.Ai;
using Netptune.Core.Requests;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.Sprints.Commands;

namespace Netptune.Ai.Execution.Handlers;

public sealed class CancelSprintChangeHandler : IAiChangeHandler
{
    private readonly IMediator Mediator;

    public CancelSprintChangeHandler(IMediator mediator)
    {
        Mediator = mediator;
    }

    public string ToolName => "propose_cancel_sprint";

    public async Task<AiAppliedChangeResult> Apply(
        AiChangeApplyContext context,
        CancellationToken cancellationToken)
    {
        var change = context.Change;
        var sprintId = AiChangePayload.ReadInt(change.Payload.RootElement, "sprintId") ?? change.EntityId;

        if (!sprintId.HasValue)
        {
            return AiChangePayload.Failure(change, "The sprint this change refers to could not be resolved.");
        }

        var request = new UpdateSprintRequest
        {
            Id = sprintId.Value,
            Status = SprintStatus.Cancelled,
        };

        var response = await Mediator.Send(new UpdateSprintCommand(request), cancellationToken);

        if (!response.IsSuccess)
        {
            return AiChangePayload.Failure(change, response.Message ?? "The sprint could not be cancelled.");
        }

        return AiChangePayload.Applied(change, sprintId);
    }
}
