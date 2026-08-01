using Mediator;

using Netptune.Core.Models.Ai;
using Netptune.Core.Requests;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.Tasks.Commands;

namespace Netptune.Ai.Execution.Handlers;

public sealed class CreateTaskChangeHandler : IAiChangeHandler
{
    private readonly IMediator Mediator;

    public CreateTaskChangeHandler(IMediator mediator)
    {
        Mediator = mediator;
    }

    public string ToolName => "propose_create_task";

    public async Task<AiAppliedChangeResult> Apply(
        AiChangeApplyContext context,
        CancellationToken cancellationToken)
    {
        var change = context.Change;
        var payload = change.Payload.RootElement;
        var request = new AddProjectTaskRequest
        {
            Name = AiChangePayload.ReadString(payload, "name") ?? string.Empty,
            Description = AiChangePayload.ReadString(payload, "description") ?? string.Empty,
            ProjectId = AiChangePayload.ReadInt(payload, "projectId"),
            DueDate = AiChangePayload.ReadDate(payload, "dueDate"),
        };

        var response = await Mediator.Send(new CreateTaskCommand(request), cancellationToken);

        if (!response.IsSuccess)
        {
            return AiChangePayload.Failure(change, response.Message ?? "The task could not be created.");
        }

        return AiChangePayload.Applied(change, response.Payload?.Id);
    }
}
