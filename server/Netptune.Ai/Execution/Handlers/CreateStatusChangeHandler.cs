using Mediator;

using Netptune.Core.Enums;
using Netptune.Core.Models.Ai;
using Netptune.Core.Requests;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.Statuses.Commands;

namespace Netptune.Ai.Execution.Handlers;

public sealed class CreateStatusChangeHandler : IAiChangeHandler
{
    private readonly IMediator Mediator;

    public CreateStatusChangeHandler(IMediator mediator)
    {
        Mediator = mediator;
    }

    public string ToolName => "propose_create_status";

    public async Task<AiAppliedChangeResult> Apply(
        AiChangeApplyContext context,
        CancellationToken cancellationToken)
    {
        var change = context.Change;
        var payload = change.Payload.RootElement;
        var name = AiChangePayload.ReadString(payload, "name");
        var rawCategory = AiChangePayload.ReadString(payload, "category");
        var hasName = !string.IsNullOrWhiteSpace(name);
        var isKnownCategory = Enum.TryParse<StatusCategory>(rawCategory, true, out var category);

        if (!hasName || !isKnownCategory)
        {
            return AiChangePayload.Failure(change, "The status details are missing from this change.");
        }

        var request = new CreateStatusRequest
        {
            Name = name!,
            Description = AiChangePayload.ReadString(payload, "description"),
            Color = AiChangePayload.ReadString(payload, "color"),
            Category = category,
        };

        var response = await Mediator.Send(new CreateStatusCommand(request), cancellationToken);

        if (!response.IsSuccess)
        {
            return AiChangePayload.Failure(change, response.Message ?? "The status could not be created.");
        }

        return AiChangePayload.Applied(change, response.Payload?.Id);
    }
}
