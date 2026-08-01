using Mediator;

using Netptune.Core.Models.Ai;
using Netptune.Core.Requests;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.Relations.Commands;

namespace Netptune.Ai.Execution.Handlers;

public sealed class LinkTasksChangeHandler : IAiChangeHandler
{
    private readonly IMediator Mediator;

    public LinkTasksChangeHandler(IMediator mediator)
    {
        Mediator = mediator;
    }

    public string ToolName => "propose_link_tasks";

    public async Task<AiAppliedChangeResult> Apply(
        AiChangeApplyContext context,
        CancellationToken cancellationToken)
    {
        var change = context.Change;
        var payload = change.Payload.RootElement;
        var sourceSystemId = AiChangePayload.ReadString(payload, "sourceSystemId");
        var targetSystemId = AiChangePayload.ReadString(payload, "targetSystemId");
        var relationTypeId = AiChangePayload.ReadInt(payload, "relationTypeId");
        var hasTasks = !string.IsNullOrWhiteSpace(sourceSystemId) && !string.IsNullOrWhiteSpace(targetSystemId);

        if (!hasTasks || !relationTypeId.HasValue)
        {
            return AiChangePayload.Failure(change, "The tasks this link refers to could not be resolved.");
        }

        var request = new CreateTaskRelationRequest
        {
            SourceSystemId = sourceSystemId!,
            TargetSystemId = targetSystemId!,
            RelationTypeId = relationTypeId.Value,
        };

        var response = await Mediator.Send(new CreateTaskRelationCommand(request), cancellationToken);

        if (!response.IsSuccess)
        {
            return AiChangePayload.Failure(change, response.Message ?? "The tasks could not be linked.");
        }

        return AiChangePayload.Applied(change, change.EntityId);
    }
}
