using Mediator;

using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Pages;

namespace Netptune.Handlers.Pages.Queries;

public sealed record GetPageTreeQuery : IRequest<ClientResponse<List<PageTreeNodeViewModel>>>;

public sealed class GetPageTreeQueryHandler : IRequestHandler<GetPageTreeQuery, ClientResponse<List<PageTreeNodeViewModel>>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public GetPageTreeQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<ClientResponse<List<PageTreeNodeViewModel>>> Handle(GetPageTreeQuery request, CancellationToken cancellationToken)
    {
        var workspaceId = await Identity.GetWorkspaceId();
        var userId = Identity.GetCurrentUserId();
        var nodes = await UnitOfWork.Pages.GetTree(workspaceId, userId, cancellationToken);

        return ClientResponse<List<PageTreeNodeViewModel>>.Success(nodes);
    }
}
