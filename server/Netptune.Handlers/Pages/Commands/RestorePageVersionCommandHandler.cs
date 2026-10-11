using Mediator;

using Netptune.Core.Enums;
using Netptune.Core.Models.Pages;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Pages;

namespace Netptune.Handlers.Pages.Commands;

public sealed record RestorePageVersionCommand(int PageId, int VersionId) : IRequest<ClientResponse<PageViewModel>>;

public sealed class RestorePageVersionCommandHandler : IRequestHandler<RestorePageVersionCommand, ClientResponse<PageViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IActivityLogger Activity;
    private readonly IEventPublisher EventPublisher;

    public RestorePageVersionCommandHandler(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        IActivityLogger activity,
        IEventPublisher eventPublisher)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Activity = activity;
        EventPublisher = eventPublisher;
    }

    public async ValueTask<ClientResponse<PageViewModel>> Handle(RestorePageVersionCommand request, CancellationToken cancellationToken)
    {
        var workspaceKey = Identity.GetWorkspaceKey();
        var workspaceId = await Identity.GetWorkspaceId();
        var userId = Identity.GetCurrentUserId();
        var page = await UnitOfWork.Pages.GetLiveInWorkspace(request.PageId, workspaceId, true, cancellationToken);

        if (page is null)
        {
            return ClientResponse<PageViewModel>.NotFound;
        }

        var version = await UnitOfWork.PageVersions.GetViewModel(request.VersionId, page.Id, workspaceId, cancellationToken);

        if (version is null)
        {
            return ClientResponse<PageViewModel>.NotFound;
        }

        var isFolder = page.Kind == PageKind.Folder;
        var restoredBody = isFolder ? null : version.Body ?? string.Empty;

        var update = new PageContentUpdate
        {
            PageId = page.Id,
            WorkspaceId = workspaceId,
            WorkspaceKey = workspaceKey,
            ExpectedRevision = page.Revision,
            Title = version.Title,
            Slug = PageSlug.Follow(page.Slug, version.Title, restoredBody),
            Body = restoredBody,
            UserId = userId,
            RestoredFromVersionId = version.Id,
        };

        var saved = await PageContentWriter.TryWrite(UnitOfWork, update, cancellationToken);
        var viewModel = await UnitOfWork.Pages.GetViewModel(page.Id, workspaceId, userId, cancellationToken);

        if (viewModel is null)
        {
            return ClientResponse<PageViewModel>.NotFound;
        }

        if (!saved)
        {
            return ClientResponse<PageViewModel>.Conflict(viewModel, SavePageCommandHandler.ConflictMessage);
        }

        var indexScope = new PageIndexScope(page.Id, workspaceId, workspaceKey);
        var renamed = page.Title != version.Title;

        await PageSearchIndex.IndexAfterSave(UnitOfWork, EventPublisher, indexScope, renamed, cancellationToken);

        Activity.Log(options =>
        {
            options.EntityId = page.Id;
            options.EntityType = EntityType.Page;
            options.Type = ActivityType.Restore;
        });

        return ClientResponse<PageViewModel>.Success(viewModel);
    }
}
