using Mediator;

using Netptune.Core.Requests;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Boards;

namespace Netptune.Handlers.Boards.Queries;

public sealed record GetBoardsInProjectQuery(int ProjectId, PageRequest? Page = null) : IRequest<List<BoardViewModel>?>;

public sealed class GetBoardsInProjectQueryHandler : IRequestHandler<GetBoardsInProjectQuery, List<BoardViewModel>?>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public GetBoardsInProjectQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<List<BoardViewModel>?> Handle(GetBoardsInProjectQuery request, CancellationToken cancellationToken)
    {
        var workspaceId = await Identity.GetWorkspaceId();
        var project = await UnitOfWork.Projects.GetInWorkspace(request.ProjectId, workspaceId, true, cancellationToken);

        if (project is null)
        {
            return null;
        }

        var results = await UnitOfWork.Boards.GetBoardsInProject(request.ProjectId, true, cancellationToken: cancellationToken, pageRequest: request.Page);

        return results.ConvertAll(r => r.ToViewModel());
    }
}
