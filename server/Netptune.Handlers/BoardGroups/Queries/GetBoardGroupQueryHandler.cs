using Mediator;
using Netptune.Core.Entities;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.BoardGroups.Queries;

public sealed record GetBoardGroupQuery(int Id) : IRequest<BoardGroup?>;

public sealed class GetBoardGroupQueryHandler : IRequestHandler<GetBoardGroupQuery, BoardGroup?>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public GetBoardGroupQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<BoardGroup?> Handle(GetBoardGroupQuery request, CancellationToken cancellationToken)
    {
        var workspaceId = await Identity.GetWorkspaceId();

        return await UnitOfWork.BoardGroups.GetInWorkspace(request.Id, workspaceId, true, cancellationToken);
    }
}
