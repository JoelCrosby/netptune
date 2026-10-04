using Mediator;

using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Boards;

namespace Netptune.Handlers.Boards.Queries;

public sealed record GetBoardQuery(int Id) : IRequest<ClientResponse<BoardViewModel>>;

public sealed class GetBoardQueryHandler : IRequestHandler<GetBoardQuery, ClientResponse<BoardViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public GetBoardQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<ClientResponse<BoardViewModel>> Handle(GetBoardQuery request, CancellationToken cancellationToken)
    {
        var workspaceKey = Identity.GetWorkspaceKey();
        var result = await UnitOfWork.Boards.GetWorkspaceBoardViewModel(workspaceKey, request.Id, cancellationToken);

        if (result is null) return ClientResponse<BoardViewModel>.NotFound;

        return ClientResponse<BoardViewModel>.Success(result);
    }
}
