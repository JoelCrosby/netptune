using Mediator;

using Netptune.Core.Models.Ai;
using Netptune.Core.Services;
using Netptune.Core.Services.Ai;

namespace Netptune.Handlers.Ai.Queries;

public sealed record GetAiModelsQuery : IRequest<List<AiModelOption>>;

public sealed class GetAiModelsQueryHandler : IRequestHandler<GetAiModelsQuery, List<AiModelOption>>
{
    private readonly IIdentityService Identity;
    private readonly IAiCredentialResolver Credentials;
    private readonly IAiModelCatalog Models;

    public GetAiModelsQueryHandler(IIdentityService identity, IAiCredentialResolver credentials, IAiModelCatalog models)
    {
        Identity = identity;
        Credentials = credentials;
        Models = models;
    }

    public async ValueTask<List<AiModelOption>> Handle(GetAiModelsQuery query, CancellationToken cancellationToken)
    {
        var userId = Identity.GetCurrentUserId();
        var workspaceId = await Identity.GetWorkspaceId();
        var credentials = await Credentials.Resolve(userId, workspaceId, cancellationToken);

        return await Models.List(credentials, cancellationToken);
    }
}
