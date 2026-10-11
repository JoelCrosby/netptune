using Mediator;

using Netptune.Core.Enums;
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

    public MovePageCommandHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity, IActivityLogger activity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Activity = activity;
    }

    public async ValueTask<ClientResponse<PageViewModel>> Handle(MovePageCommand request, CancellationToken cancellationToken)
    {
        var input = request.Request;
        var sortOrderIsNumber = double.IsFinite(input.SortOrder);

        if (!sortOrderIsNumber)
        {
            return ClientResponse<PageViewModel>.Failed("The sort order must be a number.");
        }

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
                return ClientResponse<PageViewModel>.Failed("The parent page does not exist.");
            }

            var parentLineage = await UnitOfWork.Pages.GetAncestorIds(parentId, workspaceId, cancellationToken);
            var wouldCreateCycle = parentLineage.Contains(page.Id);

            if (wouldCreateCycle)
            {
                return ClientResponse<PageViewModel>.Failed("A page cannot be moved inside itself.");
            }

            var changesSpace = parent.ProjectId != page.ProjectId;

            if (changesSpace)
            {
                return ClientResponse<PageViewModel>.Failed("Pages can't be moved between spaces yet.");
            }
        }

        page.ParentId = input.ParentId;
        page.SortOrder = input.SortOrder;
        page.ModifiedByUserId = userId;

        await UnitOfWork.CompleteAsync(cancellationToken);

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
