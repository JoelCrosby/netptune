using Mediator;

using Netptune.Core.Enums;
using Netptune.Core.Localization;
using Netptune.Core.Requests.Pages;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Pages;

namespace Netptune.Handlers.Pages.Commands;

public sealed record MovePageCommand(int Id, MovePageRequest Request) : IRequest<ClientResponse<PageViewModel>>;

public sealed class MovePageCommandHandler : IRequestHandler<MovePageCommand, ClientResponse<PageViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IActivityLogger Activity;
    private readonly IEventPublisher EventPublisher;

    public MovePageCommandHandler(
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

    public async ValueTask<ClientResponse<PageViewModel>> Handle(MovePageCommand request, CancellationToken cancellationToken)
    {
        var input = request.Request;
        var sortOrderIsNumber = double.IsFinite(input.SortOrder);

        if (!sortOrderIsNumber)
        {
            return ClientResponse<PageViewModel>.Failed(AppText.ErrorSortOrderMustNumber);
        }

        var workspaceKey = Identity.GetWorkspaceKey();
        var workspaceId = await Identity.GetWorkspaceId();
        var userId = Identity.GetCurrentUserId();
        var page = await UnitOfWork.Pages.GetLiveInWorkspace(request.Id, workspaceId, cancellationToken: cancellationToken);

        if (page is null)
        {
            return ClientResponse<PageViewModel>.NotFound;
        }

        if (input.ParentId is { } parentId)
        {
            var parent = await UnitOfWork.Pages.GetLiveInWorkspace(parentId, workspaceId, true, cancellationToken);

            if (parent is null)
            {
                return ClientResponse<PageViewModel>.Failed(AppText.ErrorParentPageDoesNotExist);
            }

            var parentLineage = await UnitOfWork.Pages.GetAncestorIds(parentId, workspaceId, cancellationToken);
            var wouldCreateCycle = parentLineage.Contains(page.Id);

            if (wouldCreateCycle)
            {
                return ClientResponse<PageViewModel>.Failed(AppText.ErrorPageCannotMovedInsideItself);
            }

            var changesSpace = parent.ProjectId != page.ProjectId;

            if (changesSpace)
            {
                return ClientResponse<PageViewModel>.Failed(AppText.ErrorPagesCannotMoveBetweenSpaces);
            }
        }

        var parentChanged = page.ParentId != input.ParentId;

        page.ParentId = input.ParentId;
        page.SortOrder = input.SortOrder;
        page.ModifiedByUserId = userId;

        await UnitOfWork.CompleteAsync(cancellationToken);

        if (parentChanged)
        {
            var indexScope = new PageIndexScope(page.Id, workspaceId, workspaceKey);

            await PageSearchIndex.IndexSubtree(UnitOfWork, EventPublisher, indexScope, cancellationToken);
        }

        var viewModel = await UnitOfWork.Pages.GetViewModel(page.Id, workspaceId, userId, cancellationToken);

        Activity.Log(options =>
        {
            options.EntityId = page.Id;
            options.EntityType = EntityType.Page;
            options.Type = ActivityType.Move;
        });

        return ClientResponse<PageViewModel>.Success(viewModel!);
    }
}
