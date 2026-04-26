using Mediator;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Boards;

namespace Netptune.Services.Boards.Queries;

public sealed class GetBoardsInProjectQueryHandler : IRequestHandler<GetBoardsInProjectQuery, List<BoardViewModel>?>
{
    private readonly INetptuneUnitOfWork UnitOfWork;

    public GetBoardsInProjectQueryHandler(INetptuneUnitOfWork unitOfWork)
    {
        UnitOfWork = unitOfWork;
    }

    public async ValueTask<List<BoardViewModel>?> Handle(GetBoardsInProjectQuery request, CancellationToken cancellationToken)
    {
        var results = await UnitOfWork.Boards.GetBoardsInProject(request.ProjectId, true);

        return results.ConvertAll(r => r.ToViewModel());
    }
}
