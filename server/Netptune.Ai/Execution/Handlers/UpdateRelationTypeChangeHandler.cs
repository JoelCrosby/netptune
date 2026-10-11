using Mediator;

using Netptune.Ai.Tools;
using Netptune.Core.Models.Ai;
using Netptune.Core.Requests;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.RelationTypes.Commands;
using Netptune.Handlers.RelationTypes.Queries;

namespace Netptune.Ai.Execution.Handlers;

public sealed class UpdateRelationTypeChangeHandler : IAiChangeHandler
{
    private readonly IMediator Mediator;

    public UpdateRelationTypeChangeHandler(IMediator mediator)
    {
        Mediator = mediator;
    }

    public string ToolName => UpdateRelationTypeAction.Change;

    public async Task<AiAppliedChangeResult> Apply(
        AiChangeApplyContext context,
        CancellationToken cancellationToken)
    {
        var change = context.Change;
        var payload = change.Payload.RootElement;
        var relationTypeId = AiChangePayload.ReadInt(payload, "relationTypeId") ?? change.EntityId;

        if (!relationTypeId.HasValue)
        {
            return AiChangePayload.Failure(change, "The relation type this change refers to could not be resolved.");
        }

        // The update command replaces every field, so fields the change leaves alone are read fresh at apply time.
        var relationTypes = await Mediator.Send(new GetRelationTypesQuery(), cancellationToken) ?? [];
        var relationType = relationTypes.FirstOrDefault(item => item.Id == relationTypeId.Value);

        if (relationType is null)
        {
            return AiChangePayload.Failure(change, "The relation type no longer exists in this workspace.");
        }

        var request = new UpdateRelationTypeRequest
        {
            Id = relationType.Id,
            Name = AiChangePayload.ReadString(payload, "name") ?? relationType.Name,
            InverseName = AiChangePayload.ReadString(payload, "inverseName") ?? relationType.InverseName,
            Description = AiChangePayload.ReadString(payload, "description") ?? relationType.Description,
            Color = AiChangePayload.ReadString(payload, "color") ?? relationType.Color,
        };

        var response = await Mediator.Send(new UpdateRelationTypeCommand(request), cancellationToken);

        if (!response.IsSuccess)
        {
            return AiChangePayload.Failure(change, response.Message ?? "The relation type could not be updated.");
        }

        return AiChangePayload.Applied(change, relationType.Id);
    }
}
