using Mediator;

using Netptune.Core.Enums;
using Netptune.Core.Models.Ai;
using Netptune.Core.Requests;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.Statuses.Commands;
using Netptune.Handlers.Statuses.Queries;

namespace Netptune.Ai.Execution.Handlers;

public sealed class UpdateStatusChangeHandler : IAiChangeHandler
{
    private readonly IMediator Mediator;

    public UpdateStatusChangeHandler(IMediator mediator)
    {
        Mediator = mediator;
    }

    public string ToolName => "propose_update_status";

    public async Task<AiAppliedChangeResult> Apply(
        AiChangeApplyContext context,
        CancellationToken cancellationToken)
    {
        var change = context.Change;
        var payload = change.Payload.RootElement;
        var statusId = AiChangePayload.ReadInt(payload, "statusId") ?? change.EntityId;

        if (!statusId.HasValue)
        {
            return AiChangePayload.Failure(change, "The status this change refers to could not be resolved.");
        }

        // The update command replaces every field, so fields the change leaves alone are read fresh at apply time.
        var statuses = await Mediator.Send(new GetStatusesQuery(new StatusFilter()), cancellationToken);
        var status = statuses?.FirstOrDefault(item => item.Id == statusId.Value);

        if (status is null)
        {
            return AiChangePayload.Failure(change, "The status no longer exists in this workspace.");
        }

        var rawCategory = AiChangePayload.ReadString(payload, "category");
        var category = status.Category;
        var hasCategory = rawCategory is not null;

        if (hasCategory)
        {
            var isKnownCategory = Enum.TryParse(rawCategory, true, out category);

            if (!isKnownCategory)
            {
                return AiChangePayload.Failure(change, $"\"{rawCategory}\" is not a status category.");
            }
        }

        var request = new UpdateStatusRequest
        {
            Id = status.Id,
            Name = AiChangePayload.ReadString(payload, "name") ?? status.Name,
            Description = AiChangePayload.ReadString(payload, "description") ?? status.Description,
            Color = AiChangePayload.ReadString(payload, "color") ?? status.Color,
            Category = category,
        };

        var response = await Mediator.Send(new UpdateStatusCommand(request), cancellationToken);

        if (!response.IsSuccess)
        {
            return AiChangePayload.Failure(change, response.Message ?? "The status could not be updated.");
        }

        return AiChangePayload.Applied(change, status.Id);
    }
}
