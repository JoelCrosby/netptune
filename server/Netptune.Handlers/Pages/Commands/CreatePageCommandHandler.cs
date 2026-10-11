using Mediator;

using Netptune.Core.Encoding;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Models.Pages;
using Netptune.Core.Models.Search;
using Netptune.Core.Requests.Pages;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Pages;

namespace Netptune.Handlers.Pages.Commands;

public sealed record CreatePageCommand(CreatePageRequest Request) : IRequest<ClientResponse<PageViewModel>>;

public sealed class CreatePageCommandHandler : IRequestHandler<CreatePageCommand, ClientResponse<PageViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IActivityLogger Activity;
    private readonly IEventPublisher EventPublisher;

    public CreatePageCommandHandler(
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

    public async ValueTask<ClientResponse<PageViewModel>> Handle(CreatePageCommand request, CancellationToken cancellationToken)
    {
        var input = request.Request;
        var isKnownKind = Enum.IsDefined(input.Kind);

        if (!isKnownKind)
        {
            return ClientResponse<PageViewModel>.Failed("Unknown page kind.");
        }

        var title = PageTitle.Normalise(input.Title);
        var titleTooLong = PageTitle.IsTooLong(title);

        if (titleTooLong)
        {
            return ClientResponse<PageViewModel>.Failed(PageTitle.TooLongMessage);
        }

        var workspaceKey = Identity.GetWorkspaceKey();
        var workspaceId = await Identity.GetWorkspaceId();
        var userId = Identity.GetCurrentUserId();
        var parent = await ResolveParent(input.ParentId, workspaceId, cancellationToken);
        var parentIsMissing = input.ParentId.HasValue && parent is null;

        if (parentIsMissing)
        {
            return ClientResponse<PageViewModel>.Failed("The parent page does not exist.");
        }

        var sortOrder = await UnitOfWork.Pages.GetNextSortOrder(workspaceId, input.ParentId, cancellationToken);
        var isDocument = input.Kind == PageKind.Document;

        var page = new Page
        {
            Kind = input.Kind,
            ParentId = input.ParentId,
            ProjectId = parent?.ProjectId,
            Title = title,
            Slug = title.ToUrlSlug(true, PageTree.SlugMaxLength),
            Body = isDocument ? string.Empty : null,
            SortOrder = sortOrder,
            Status = PageStatus.Draft,
            Revision = 1,
            WorkspaceId = workspaceId,
            CreatedByUserId = userId,
            OwnerId = userId,
        };

        await UnitOfWork.Transaction(async () =>
        {
            await UnitOfWork.Pages.AddAsync(page, cancellationToken);
            await UnitOfWork.CompleteAsync(cancellationToken);

            var firstVersion = new PageVersionSnapshot
            {
                PageId = page.Id,
                Revision = page.Revision,
                Title = page.Title,
                Body = page.Body,
                UserId = userId,
            };

            var version = PageVersionRecorder.ToVersion(firstVersion, DateTime.UtcNow);

            await UnitOfWork.PageVersions.AddAsync(version, cancellationToken);
            await UnitOfWork.CompleteAsync(cancellationToken);
        });

        await EventPublisher.IndexPages([page.Id], workspaceKey);

        var viewModel = await UnitOfWork.Pages.GetViewModel(page.Id, workspaceId, userId, cancellationToken);

        Activity.Log(options =>
        {
            options.EntityId = page.Id;
            options.EntityType = EntityType.Page;
            options.Type = ActivityType.Create;
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
