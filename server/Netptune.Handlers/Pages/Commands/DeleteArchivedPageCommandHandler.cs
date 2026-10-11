using Mediator;

using Netptune.Core.Cache;
using Netptune.Core.Enums;
using Netptune.Core.Models.Search;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.Pages.Commands;

public sealed record DeleteArchivedPageCommand(int Id) : IRequest<ClientResponse>;

public sealed class DeleteArchivedPageCommandHandler : IRequestHandler<DeleteArchivedPageCommand, ClientResponse>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IWorkspacePermissionCache PermissionCache;
    private readonly IActivityLogger Activity;
    private readonly IEventPublisher EventPublisher;

    public DeleteArchivedPageCommandHandler(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        IWorkspacePermissionCache permissionCache,
        IActivityLogger activity,
        IEventPublisher eventPublisher)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        PermissionCache = permissionCache;
        Activity = activity;
        EventPublisher = eventPublisher;
    }

    public async ValueTask<ClientResponse> Handle(DeleteArchivedPageCommand request, CancellationToken cancellationToken)
    {
        var workspaceKey = Identity.GetWorkspaceKey();
        var workspaceId = await Identity.GetWorkspaceId();
        var userId = Identity.GetCurrentUserId();
        var page = await UnitOfWork.Pages.GetArchivedInWorkspace(request.Id, workspaceId, cancellationToken);

        if (page is null)
        {
            return ClientResponse.NotFound;
        }

        var rights = await PagePermissions.GetArchiveRights(PermissionCache, Identity, userId, workspaceKey);
        var isOwnPage = page.CreatedByUserId == userId;
        var mayDeleteRoot = rights.Any || (rights.Own && isOwnPage);

        if (!mayDeleteRoot)
        {
            return ClientResponse.Forbidden;
        }

        var subtreeIds = await UnitOfWork.Pages.GetSubtreeIds(page.Id, workspaceId, true, cancellationToken);

        if (!rights.Any)
        {
            var holdsOthersPages = await UnitOfWork.Pages.AnyCreatedByOthers(subtreeIds, userId, cancellationToken);

            if (holdsOthersPages)
            {
                return ClientResponse.Failed(PagePermissions.OthersPagesInsideMessage);
            }
        }

        var deletedIds = await UnitOfWork.Transaction(() => UnitOfWork.Pages.DeleteArchived(subtreeIds, workspaceId, cancellationToken));

        await EventPublisher.RemovePages(deletedIds, workspaceKey);

        Activity.Log(options =>
        {
            options.EntityId = page.Id;
            options.EntityType = EntityType.Page;
            options.Type = ActivityType.Remove;
        });

        return ClientResponse.Success;
    }
}
