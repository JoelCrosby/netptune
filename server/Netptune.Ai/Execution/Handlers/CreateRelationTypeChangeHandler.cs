using Mediator;

using Netptune.Ai.Tools;
using Netptune.Core.Enums;
using Netptune.Core.Localization;
using Netptune.Core.Models.Ai;
using Netptune.Core.Requests;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.RelationTypes.Commands;

namespace Netptune.Ai.Execution.Handlers;

public sealed class CreateRelationTypeChangeHandler : IAiChangeHandler
{
    private readonly IMediator Mediator;

    public CreateRelationTypeChangeHandler(IMediator mediator)
    {
        Mediator = mediator;
    }

    public string ToolName => "propose_create_relation_type";

    public async Task<AiAppliedChangeResult> Apply(
        AiChangeApplyContext context,
        CancellationToken cancellationToken)
    {
        var change = context.Change;
        var payload = change.Payload.RootElement;
        var name = AiChangePayload.ReadString(payload, "name");
        var rawCategory = AiChangePayload.ReadString(payload, "category");
        var hasName = !string.IsNullOrWhiteSpace(name);
        var isKnownCategory = AiToolSchema.TryParseEnum<RelationCategory>(rawCategory, out var category);

        if (!hasName || !isKnownCategory)
        {
            return AiChangePayload.Failure(change, AppText.AiChangeErrorRelationTypeDetailsMissingChange);
        }

        var request = new CreateRelationTypeRequest
        {
            Name = name!,
            InverseName = AiChangePayload.ReadString(payload, "inverseName"),
            Description = AiChangePayload.ReadString(payload, "description"),
            Color = AiChangePayload.ReadString(payload, "color"),
            Category = category,
        };

        var response = await Mediator.Send(new CreateRelationTypeCommand(request), cancellationToken);

        if (!response.IsSuccess)
        {
            return AiChangePayload.Failure(
                change,
                response.Message ?? AppText.AiChangeErrorRelationTypeCouldNotCreated);
        }

        return AiChangePayload.Applied(change, response.Payload?.Id);
    }
}
