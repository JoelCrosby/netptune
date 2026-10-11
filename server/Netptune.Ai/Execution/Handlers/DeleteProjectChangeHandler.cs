using Mediator;

using Netptune.Ai.Tools;
using Netptune.Core.Models.Ai;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.Projects.Commands;

namespace Netptune.Ai.Execution.Handlers;

public sealed class DeleteProjectChangeHandler : IAiChangeHandler
{
    private readonly IMediator Mediator;

    public DeleteProjectChangeHandler(IMediator mediator)
    {
        Mediator = mediator;
    }

    public string ToolName => DeleteProjectAction.Change;

    public async Task<AiAppliedChangeResult> Apply(
        AiChangeApplyContext context,
        CancellationToken cancellationToken)
    {
        var change = context.Change;
        var projectId = AiChangePayload.ReadInt(change.Payload.RootElement, "projectId") ?? change.EntityId;

        if (!projectId.HasValue)
        {
            return AiChangePayload.Failure(change, "The project this change refers to could not be resolved.");
        }

        var response = await Mediator.Send(new DeleteProjectCommand(projectId.Value), cancellationToken);

        if (!response.IsSuccess)
        {
            return AiChangePayload.Failure(change, response.Message ?? "The project could not be deleted.");
        }

        return AiChangePayload.Applied(change, projectId.Value);
    }
}
