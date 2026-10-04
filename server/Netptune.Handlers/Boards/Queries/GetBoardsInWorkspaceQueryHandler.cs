using Mediator;

using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Boards;

namespace Netptune.Handlers.Boards.Queries;

public sealed record GetBoardsInWorkspaceQuery(BoardFilter Filter) : IRequest<ClientResponse<PagedResponse<BoardViewModel>>>;

public sealed class GetBoardsInWorkspaceQueryHandler : IRequestHandler<GetBoardsInWorkspaceQuery, ClientResponse<PagedResponse<BoardViewModel>>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public GetBoardsInWorkspaceQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<ClientResponse<PagedResponse<BoardViewModel>>> Handle(GetBoardsInWorkspaceQuery request, CancellationToken cancellationToken)
    {
        var workspaceKey = Identity.GetWorkspaceKey();
        var workspaceExists = await UnitOfWork.Workspaces.Exists(workspaceKey, cancellationToken);

        if (!workspaceExists)
        {
            return ClientResponse<PagedResponse<BoardViewModel>>.NotFound;
        }

        var page = await UnitOfWork.Boards.GetBoardsPage(workspaceKey, request.Filter, cancellationToken);

        return ClientResponse<PagedResponse<BoardViewModel>>.Success(page);
    }
}
