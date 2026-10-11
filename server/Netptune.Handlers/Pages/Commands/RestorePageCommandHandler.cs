using Mediator;

using Netptune.Core.Cache;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Pages;

namespace Netptune.Handlers.Pages.Commands;

public sealed record RestorePageCommand(int Id) : IRequest<ClientResponse<PageViewModel>>;

public sealed class RestorePageCommandHandler : IRequestHandler<RestorePageCommand, ClientResponse<PageViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IWorkspacePermissionCache PermissionCache;
    private readonly IActivityLogger Activity;

    public RestorePageCommandHandler(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        IWorkspacePermissionCache permissionCache,
        IActivityLogger activity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        PermissionCache = permissionCache;
        Activity = activity;
    }

    public async ValueTask<ClientResponse<PageViewModel>> Handle(RestorePageCommand request, CancellationToken cancellationToken)
    {
        var workspaceKey = Identity.GetWorkspaceKey();
        var workspaceId = await Identity.GetWorkspaceId();
        var userId = Identity.GetCurrentUserId();
        var page = await UnitOfWork.Pages.GetArchivedInWorkspace(request.Id, workspaceId, cancellationToken);

        if (page is null)
        {
            return ClientResponse<PageViewModel>.NotFound;
        }

        var rights = await PagePermissions.GetArchiveRights(PermissionCache, userId, workspaceKey);
        var isOwnPage = page.CreatedByUserId == userId;
        var mayRestoreRoot = rights.Any || (rights.Own && isOwnPage);

        if (!mayRestoreRoot)
        {
            return ClientResponse<PageViewModel>.Forbidden;
        }

        var restoredIds = await UnitOfWork.Pages.GetArchivedTogetherIds(page, cancellationToken);

        if (!rights.Any)
        {
            var holdsOthersPages = await UnitOfWork.Pages.AnyCreatedByOthers(restoredIds, userId, cancellationToken);

            if (holdsOthersPages)
            {
                return ClientResponse<PageViewModel>.Failed(PagePermissions.OthersPagesInsideMessage);
            }
        }

        var parent = await ResolveParent(page.ParentId, workspaceId, cancellationToken);
        var parentIsGone = page.ParentId.HasValue && parent is null;

        await UnitOfWork.Transaction(async () =>
        {
            if (parentIsGone)
            {
                var rootSortOrder = await UnitOfWork.Pages.GetNextSortOrder(workspaceId, null, cancellationToken);

                page.ParentId = null;
                page.SortOrder = rootSortOrder;

                await UnitOfWork.CompleteAsync(cancellationToken);
            }

            await UnitOfWork.Pages.Restore(restoredIds, cancellationToken);
        });

        var viewModel = await UnitOfWork.Pages.GetViewModel(page.Id, workspaceId, userId, cancellationToken);

        Activity.Log(options =>
        {
            options.EntityId = page.Id;
            options.EntityType = EntityType.Page;
            options.Type = ActivityType.Restore;
        });

        return ClientResponse<PageViewModel>.Success(viewModel!);
    }

    private async Task<Page?> ResolveParent(int? parentId, int workspaceId, CancellationToken cancellationToken)
    {
        if (parentId is null)
        {
            return null;
        }

        return await UnitOfWork.Pages.GetLiveInWorkspace(parentId.Value, workspaceId, true, cancellationToken);
    }
}
