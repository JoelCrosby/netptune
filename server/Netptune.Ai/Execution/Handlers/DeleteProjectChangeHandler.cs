using Mediator;

using Netptune.Ai.Tools;
using Netptune.Core.Localization;
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
            return AiChangePayload.Failure(change, AppText.AiChangeErrorProjectChangeRefersCouldNotResolved);
        }

        var response = await Mediator.Send(new DeleteProjectCommand(projectId.Value), cancellationToken);

        if (!response.IsSuccess)
        {
            return AiChangePayload.Failure(change, response.Message ?? AppText.AiChangeErrorProjectCouldNotDeleted);
        }

        return AiChangePayload.Applied(change, projectId.Value);
    }
}
