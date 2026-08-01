using Mediator;

using Netptune.Core.Models.Ai;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.Sprints.Commands;

namespace Netptune.Ai.Execution.Handlers;

public sealed class CompleteSprintChangeHandler : IAiChangeHandler
{
    private readonly IMediator Mediator;

    public CompleteSprintChangeHandler(IMediator mediator)
    {
        Mediator = mediator;
    }

    public string ToolName => "propose_complete_sprint";

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

        var response = await Mediator.Send(new CompleteSprintCommand(sprintId.Value), cancellationToken);

        if (!response.IsSuccess)
        {
            return AiChangePayload.Failure(change, response.Message ?? "The sprint could not be completed.");
        }

        return AiChangePayload.Applied(change, sprintId);
    }
}
