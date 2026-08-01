using Mediator;

using Netptune.Core.Models.Ai;
using Netptune.Core.Requests;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.Sprints.Commands;

namespace Netptune.Ai.Execution.Handlers;

public sealed class CreateSprintChangeHandler : IAiChangeHandler
{
    private readonly IMediator Mediator;

    public CreateSprintChangeHandler(IMediator mediator)
    {
        Mediator = mediator;
    }

    public string ToolName => "propose_create_sprint";

    public async Task<AiAppliedChangeResult> Apply(
        AiChangeApplyContext context,
        CancellationToken cancellationToken)
    {
        var change = context.Change;
        var payload = change.Payload.RootElement;
        var name = AiChangePayload.ReadString(payload, "name");
        var projectId = AiChangePayload.ReadInt(payload, "projectId");
        var startDate = AiChangePayload.ReadDate(payload, "startDate");
        var endDate = AiChangePayload.ReadDate(payload, "endDate");
        var hasName = !string.IsNullOrWhiteSpace(name);
        var hasDates = startDate.HasValue && endDate.HasValue;

        if (!hasName || !projectId.HasValue || !hasDates)
        {
            return AiChangePayload.Failure(change, "The sprint details are missing from this change.");
        }

        var request = new AddSprintRequest
        {
            Name = name!,
            Goal = AiChangePayload.ReadString(payload, "goal"),
            ProjectId = projectId.Value,
            StartDate = startDate!.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            EndDate = endDate!.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
        };

        var response = await Mediator.Send(new CreateSprintCommand(request), cancellationToken);

        if (!response.IsSuccess)
        {
            return AiChangePayload.Failure(change, response.Message ?? "The sprint could not be created.");
        }

        return AiChangePayload.Applied(change, response.Payload?.Id);
    }
}
