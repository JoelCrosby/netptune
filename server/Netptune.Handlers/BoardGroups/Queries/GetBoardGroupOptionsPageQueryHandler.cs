using Mediator;

using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Boards;

namespace Netptune.Handlers.BoardGroups.Queries;

public sealed record GetBoardGroupOptionsPageQuery(BoardGroupOptionFilter Filter)
    : IRequest<ClientResponse<PagedResponse<BoardGroupOptionViewModel>>>;

public sealed class GetBoardGroupOptionsPageQueryHandler
    : IRequestHandler<GetBoardGroupOptionsPageQuery, ClientResponse<PagedResponse<BoardGroupOptionViewModel>>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public GetBoardGroupOptionsPageQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<ClientResponse<PagedResponse<BoardGroupOptionViewModel>>> Handle(
        GetBoardGroupOptionsPageQuery request,
        CancellationToken cancellationToken)
    {
        var workspaceId = await Identity.GetWorkspaceId();
        var page = await UnitOfWork.BoardGroups.GetOptionsPage(workspaceId, request.Filter, cancellationToken);

        return ClientResponse<PagedResponse<BoardGroupOptionViewModel>>.Success(page);
    }
}
