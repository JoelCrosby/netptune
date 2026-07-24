using Mediator;

using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Boards;

namespace Netptune.Handlers.BoardGroups.Queries;

public sealed record GetBoardGroupOptionsQuery : IRequest<List<BoardGroupOptionViewModel>>;

public sealed class GetBoardGroupOptionsQueryHandler
    : IRequestHandler<GetBoardGroupOptionsQuery, List<BoardGroupOptionViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public GetBoardGroupOptionsQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<List<BoardGroupOptionViewModel>> Handle(
        GetBoardGroupOptionsQuery request,
        CancellationToken cancellationToken)
    {
        var workspaceId = await Identity.GetWorkspaceId();
        var options = await UnitOfWork.BoardGroups.GetOptionsInWorkspace(workspaceId, cancellationToken);

        return options;
    }
}
