using Mediator;

using Netptune.Core.Models.Ai;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Ai;

namespace Netptune.Handlers.Ai.Queries;

public sealed record GetAiCredentialAvailabilityQuery : IRequest<AiCredentialAvailabilityViewModel>;

public sealed class GetAiCredentialAvailabilityQueryHandler
    : IRequestHandler<GetAiCredentialAvailabilityQuery, AiCredentialAvailabilityViewModel>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public GetAiCredentialAvailabilityQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<AiCredentialAvailabilityViewModel> Handle(
        GetAiCredentialAvailabilityQuery query,
        CancellationToken cancellationToken)
    {
        var userId = Identity.GetCurrentUserId();
        var workspaceId = await Identity.GetWorkspaceId();

        var userCredentials = await UnitOfWork.AiCredentials.GetForUser(userId, cancellationToken);
        var workspaceCredentials = await UnitOfWork.WorkspaceAiCredentials.GetForWorkspace(
            workspaceId,
            cancellationToken);

        var resolved = AiCredentialResolution.Resolve(userCredentials, workspaceCredentials);

        return new AiCredentialAvailabilityViewModel
        {
            Providers = resolved
                .Select(credential => new AiProviderAvailabilityViewModel
                {
                    Provider = credential.Provider,
                    Source = credential.Source,
                    Model = credential.Model,
                })
                .ToList(),
        };
    }
}
