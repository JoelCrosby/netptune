using Mediator;

using Netptune.Core.Enums;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.Storage.Commands;

public sealed record RemoveBrandingImageCommand(BrandingImageTarget Target, int? TargetId) : IRequest<ClientResponse>;

public sealed class RemoveBrandingImageCommandHandler : IRequestHandler<RemoveBrandingImageCommand, ClientResponse>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IStorageService Storage;

    public RemoveBrandingImageCommandHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity, IStorageService storage)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Storage = storage;
    }

    public async ValueTask<ClientResponse> Handle(RemoveBrandingImageCommand request, CancellationToken cancellationToken)
    {
        var workspaceId = await Identity.GetWorkspaceId();
        var slot = await BrandingSlots.Resolve(UnitOfWork, request.Target, request.TargetId, workspaceId, cancellationToken);

        if (slot is null)
        {
            return ClientResponse.NotFound;
        }

        if (slot.CurrentFileId is null)
        {
            return ClientResponse.Success;
        }

        var file = await UnitOfWork.WorkspaceFiles.GetByContentId(slot.CurrentFileId, workspaceId, isReadonly: true, cancellationToken);

        await slot.Assign(null, cancellationToken);

        if (file is null)
        {
            return ClientResponse.Success;
        }

        var userId = Identity.GetCurrentUserId();

        await BrandingFileRelease.Release(UnitOfWork, Storage, file, userId, cancellationToken);

        return ClientResponse.Success;
    }
}
