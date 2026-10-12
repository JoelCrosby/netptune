using Mediator;

using Netptune.Core.Models.Ai;
using Netptune.Core.Services;
using Netptune.Core.Services.Ai;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.Ai.Queries;

public sealed record GetAiModelsQuery : IRequest<List<AiModelOption>>;

public sealed class GetAiModelsQueryHandler : IRequestHandler<GetAiModelsQuery, List<AiModelOption>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IAiModelCatalog Models;

    public GetAiModelsQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity, IAiModelCatalog models)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Models = models;
    }

    public async ValueTask<List<AiModelOption>> Handle(GetAiModelsQuery query, CancellationToken cancellationToken)
    {
        var userId = Identity.GetCurrentUserId();
        var workspaceId = await Identity.GetWorkspaceId();

        var userCredentials = await UnitOfWork.AiCredentials.GetForUser(userId, cancellationToken);
        var workspaceCredentials = await UnitOfWork.WorkspaceAiCredentials.GetForWorkspace(
            workspaceId,
            cancellationToken);

        var credentials = AiCredentialResolution.Resolve(userCredentials, workspaceCredentials);

        return await Models.List(credentials, cancellationToken);
    }
}
