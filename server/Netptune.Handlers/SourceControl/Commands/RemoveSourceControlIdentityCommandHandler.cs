using Mediator;

using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.SourceControl.Commands;

public sealed record RemoveSourceControlIdentityCommand(int IdentityId) : IRequest<ClientResponse>;

public sealed class RemoveSourceControlIdentityCommandHandler
    : IRequestHandler<RemoveSourceControlIdentityCommand, ClientResponse>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public RemoveSourceControlIdentityCommandHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<ClientResponse> Handle(
        RemoveSourceControlIdentityCommand command,
        CancellationToken cancellationToken)
    {
        var userId = Identity.GetCurrentUserId();
        var identity = await UnitOfWork.UserSourceControlIdentities.GetOwned(
            command.IdentityId,
            userId,
            cancellationToken);

        if (identity is null)
        {
            return ClientResponse.NotFound;
        }

        await UnitOfWork.UserSourceControlIdentities.DeletePermanent([identity], cancellationToken);
        await UnitOfWork.CompleteAsync(cancellationToken);

        return ClientResponse.Success;
    }
}
