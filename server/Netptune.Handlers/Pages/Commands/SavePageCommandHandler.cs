using Mediator;

using Netptune.Core.Enums;
using Netptune.Core.Models.Pages;
using Netptune.Core.Requests.Pages;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Pages;

namespace Netptune.Handlers.Pages.Commands;

public sealed record SavePageCommand(int Id, SavePageRequest Request) : IRequest<ClientResponse<PageViewModel>>;

public sealed class SavePageCommandHandler : IRequestHandler<SavePageCommand, ClientResponse<PageViewModel>>
{
    public const string ConflictMessage = "This page was changed by someone else.";

    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IActivityLogger Activity;
    private readonly IEventPublisher EventPublisher;

    public SavePageCommandHandler(
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

    public async ValueTask<ClientResponse<PageViewModel>> Handle(SavePageCommand request, CancellationToken cancellationToken)
    {
        var input = request.Request;
        var workspaceKey = Identity.GetWorkspaceKey();
        var workspaceId = await Identity.GetWorkspaceId();
        var userId = Identity.GetCurrentUserId();
        var page = await UnitOfWork.Pages.GetLiveInWorkspace(request.Id, workspaceId, true, cancellationToken);

        if (page is null)
        {
            return ClientResponse<PageViewModel>.NotFound;
        }

        var isFolder = page.Kind == PageKind.Folder;
        var hasBody = !string.IsNullOrEmpty(input.Body);

        if (isFolder && hasBody)
        {
            return ClientResponse<PageViewModel>.Failed("Folders have no body.");
        }

        var title = PageTitle.Normalise(input.Title);
        var titleTooLong = PageTitle.IsTooLong(title);

        if (titleTooLong)
        {
            return ClientResponse<PageViewModel>.Failed(PageTitle.TooLongMessage);
        }

        var bodyTooLarge = PageBody.IsTooLarge(input.Body);

        if (bodyTooLarge)
        {
            return ClientResponse<PageViewModel>.Failed(PageBody.TooLargeMessage);
        }

        var body = isFolder ? null : input.Body ?? string.Empty;
        var update = new PageContentUpdate
        {
            PageId = page.Id,
            WorkspaceId = workspaceId,
            WorkspaceKey = workspaceKey,
            ExpectedRevision = input.Revision,
            Title = title,
            Slug = PageSlug.Follow(page.Slug, title, body),
            Body = body,
            UserId = userId,
        };

        var saved = await PageContentWriter.TryWrite(UnitOfWork, update, cancellationToken);
        var viewModel = await UnitOfWork.Pages.GetViewModel(page.Id, workspaceId, userId, cancellationToken);

        if (viewModel is null)
        {
            return ClientResponse<PageViewModel>.NotFound;
        }

        if (!saved)
        {
            return ClientResponse<PageViewModel>.Conflict(viewModel, ConflictMessage);
        }

        var indexScope = new PageIndexScope(page.Id, workspaceId, workspaceKey);
        var renamed = page.Title != title;

        await PageSearchIndex.IndexAfterSave(UnitOfWork, EventPublisher, indexScope, renamed, cancellationToken);

        Activity.Log(options =>
        {
            options.EntityId = page.Id;
            options.EntityType = EntityType.Page;
            options.Type = ActivityType.Modify;
        });

        return ClientResponse<PageViewModel>.Success(viewModel);
    }
}
