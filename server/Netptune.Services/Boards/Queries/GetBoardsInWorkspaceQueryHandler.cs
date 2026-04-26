using Mediator;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Boards;

namespace Netptune.Services.Boards.Queries;

public sealed record GetBoardsInWorkspaceQuery : IRequest<List<BoardsViewModel>?>;

public sealed class GetBoardsInWorkspaceQueryHandler : IRequestHandler<GetBoardsInWorkspaceQuery, List<BoardsViewModel>?>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public GetBoardsInWorkspaceQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<List<BoardsViewModel>?> Handle(GetBoardsInWorkspaceQuery request, CancellationToken cancellationToken)
    {
        var workspaceKey = Identity.GetWorkspaceKey();
        var workspaceExists = await UnitOfWork.Workspaces.Exists(workspaceKey);

        if (!workspaceExists) return null;

        return await UnitOfWork.Boards.GetBoardViewModels(workspaceKey);
    }
}
