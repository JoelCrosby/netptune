using Mediator;

using Netptune.Core.Models.Ai;
using Netptune.Core.Requests;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.Relations.Commands;
using Netptune.Handlers.Tasks.Queries;

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
        var sourceSystemId = await ResolveSystemId(context, "sourceSystemId", "sourceRef", cancellationToken);
        var targetSystemId = await ResolveSystemId(context, "targetSystemId", "targetRef", cancellationToken);
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

    private async Task<string?> ResolveSystemId(
        AiChangeApplyContext context,
        string systemIdName,
        string refName,
        CancellationToken cancellationToken)
    {
        var systemId = AiChangePayload.ReadString(context.Change.Payload.RootElement, systemIdName);

        if (!string.IsNullOrWhiteSpace(systemId))
        {
            return systemId;
        }

        var taskId = AiChangePayload.ResolveReference(context, refName);

        if (!taskId.HasValue)
        {
            return null;
        }

        var task = await Mediator.Send(new GetTaskQuery(taskId.Value), cancellationToken);

        return task?.SystemId;
    }
}
