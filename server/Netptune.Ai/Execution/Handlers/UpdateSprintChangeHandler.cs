using Mediator;

using Netptune.Core.Models.Ai;
using Netptune.Core.Requests;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.Sprints.Commands;

namespace Netptune.Ai.Execution.Handlers;

public sealed class UpdateSprintChangeHandler : IAiChangeHandler
{
    private readonly IMediator Mediator;

    public UpdateSprintChangeHandler(IMediator mediator)
    {
        Mediator = mediator;
    }

    public string ToolName => "propose_update_sprint";

    public async Task<AiAppliedChangeResult> Apply(
        AiChangeApplyContext context,
        CancellationToken cancellationToken)
    {
        var change = context.Change;
        var payload = change.Payload.RootElement;
        var sprintId = AiChangePayload.ReadInt(payload, "sprintId") ?? change.EntityId;

        if (!sprintId.HasValue)
        {
            return AiChangePayload.Failure(change, "The sprint this change refers to could not be resolved.");
        }

        var startDate = AiChangePayload.ReadDate(payload, "startDate");
        var endDate = AiChangePayload.ReadDate(payload, "endDate");
        var request = new UpdateSprintRequest
        {
            Id = sprintId.Value,
            Name = AiChangePayload.ReadString(payload, "name"),
            Goal = AiChangePayload.ReadString(payload, "goal"),
            StartDate = startDate?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            EndDate = endDate?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
        };

        var response = await Mediator.Send(new UpdateSprintCommand(request), cancellationToken);

        if (!response.IsSuccess)
        {
            return AiChangePayload.Failure(change, response.Message ?? "The sprint could not be updated.");
        }

        return AiChangePayload.Applied(change, sprintId);
    }
}
