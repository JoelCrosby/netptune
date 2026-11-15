using Mediator;

using Netptune.Core.Services;
using Netptune.Core.Services.Ai;
using Netptune.Core.ViewModels.Ai;

namespace Netptune.Handlers.Ai.Queries;

public sealed record GetAiCredentialAvailabilityQuery : IRequest<AiCredentialAvailabilityViewModel>;

public sealed class GetAiCredentialAvailabilityQueryHandler
    : IRequestHandler<GetAiCredentialAvailabilityQuery, AiCredentialAvailabilityViewModel>
{
    private readonly IIdentityService Identity;
    private readonly IAiCredentialResolver Credentials;

    public GetAiCredentialAvailabilityQueryHandler(IIdentityService identity, IAiCredentialResolver credentials)
    {
        Identity = identity;
        Credentials = credentials;
    }

    public async ValueTask<AiCredentialAvailabilityViewModel> Handle(
        GetAiCredentialAvailabilityQuery query,
        CancellationToken cancellationToken)
    {
        var userId = Identity.GetCurrentUserId();
        var workspaceId = await Identity.GetWorkspaceId();
        var resolved = await Credentials.Resolve(userId, workspaceId, cancellationToken);

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
