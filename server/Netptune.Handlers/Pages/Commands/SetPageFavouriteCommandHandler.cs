using Mediator;

using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.Pages.Commands;

public sealed record SetPageFavouriteCommand(int Id, bool IsFavourite) : IRequest<ClientResponse>;

public sealed class SetPageFavouriteCommandHandler : IRequestHandler<SetPageFavouriteCommand, ClientResponse>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public SetPageFavouriteCommandHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<ClientResponse> Handle(SetPageFavouriteCommand request, CancellationToken cancellationToken)
    {
        var workspaceId = await Identity.GetWorkspaceId();
        var userId = Identity.GetCurrentUserId();
        var page = await UnitOfWork.Pages.GetLiveInWorkspace(request.Id, workspaceId, true, cancellationToken);

        if (page is null)
        {
            return ClientResponse.NotFound;
        }

        if (!request.IsFavourite)
        {
            await UnitOfWork.PageFavourites.Remove(page.Id, userId, cancellationToken);

            return ClientResponse.Success;
        }

        await UnitOfWork.PageFavourites.Add(page.Id, userId, cancellationToken);

        return ClientResponse.Success;
    }
}
