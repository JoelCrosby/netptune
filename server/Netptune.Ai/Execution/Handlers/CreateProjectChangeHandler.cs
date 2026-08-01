using Mediator;

using Netptune.Core.Meta;
using Netptune.Core.Models.Ai;
using Netptune.Core.Requests;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.Projects.Commands;

namespace Netptune.Ai.Execution.Handlers;

public sealed class CreateProjectChangeHandler : IAiChangeHandler
{
    private readonly IMediator Mediator;

    public CreateProjectChangeHandler(IMediator mediator)
    {
        Mediator = mediator;
    }

    public string ToolName => "propose_create_project";

    public async Task<AiAppliedChangeResult> Apply(
        AiChangeApplyContext context,
        CancellationToken cancellationToken)
    {
        var change = context.Change;
        var payload = change.Payload.RootElement;
        var name = AiChangePayload.ReadString(payload, "name");
        var hasName = !string.IsNullOrWhiteSpace(name);

        if (!hasName)
        {
            return AiChangePayload.Failure(change, "The project name is missing from this change.");
        }

        var request = new AddProjectRequest
        {
            Name = name!,
            Description = AiChangePayload.ReadString(payload, "description"),
            RepositoryUrl = AiChangePayload.ReadString(payload, "repositoryUrl"),
            MetaInfo = new ProjectMeta(),
        };

        var response = await Mediator.Send(new CreateProjectCommand(request), cancellationToken);

        if (!response.IsSuccess)
        {
            return AiChangePayload.Failure(change, response.Message ?? "The project could not be created.");
        }

        return AiChangePayload.Applied(change, response.Payload?.Id);
    }
}
