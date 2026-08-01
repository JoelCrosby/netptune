using Mediator;

using Netptune.Core.Models.Ai;
using Netptune.Core.Requests;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.Projects.Commands;

namespace Netptune.Ai.Execution.Handlers;

public sealed class UpdateProjectChangeHandler : IAiChangeHandler
{
    private readonly IMediator Mediator;

    public UpdateProjectChangeHandler(IMediator mediator)
    {
        Mediator = mediator;
    }

    public string ToolName => "propose_update_project";

    public async Task<AiAppliedChangeResult> Apply(
        AiChangeApplyContext context,
        CancellationToken cancellationToken)
    {
        var change = context.Change;
        var payload = change.Payload.RootElement;
        var projectId = AiChangePayload.ReadInt(payload, "projectId") ?? change.EntityId;

        if (!projectId.HasValue)
        {
            return AiChangePayload.Failure(change, "The project this change refers to could not be resolved.");
        }

        var request = new UpdateProjectRequest
        {
            Id = projectId.Value,
            Name = AiChangePayload.ReadString(payload, "name"),
            Description = AiChangePayload.ReadString(payload, "description"),
            RepositoryUrl = AiChangePayload.ReadString(payload, "repositoryUrl"),
        };

        var response = await Mediator.Send(new UpdateProjectCommand(request), cancellationToken);

        if (!response.IsSuccess)
        {
            return AiChangePayload.Failure(change, response.Message ?? "The project could not be updated.");
        }

        return AiChangePayload.Applied(change, projectId);
    }
}
