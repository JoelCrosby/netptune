using Mediator;
using Netptune.Core.Entities;
using Netptune.Core.UnitOfWork;

namespace Netptune.Services.BoardGroups.Queries;

public sealed record GetBoardGroupQuery(int Id) : IRequest<BoardGroup?>;

public sealed class GetBoardGroupQueryHandler : IRequestHandler<GetBoardGroupQuery, BoardGroup?>
{
    private readonly INetptuneUnitOfWork UnitOfWork;

    public GetBoardGroupQueryHandler(INetptuneUnitOfWork unitOfWork)
    {
        UnitOfWork = unitOfWork;
    }

    public ValueTask<BoardGroup?> Handle(GetBoardGroupQuery request, CancellationToken cancellationToken)
    {
        return new ValueTask<BoardGroup?>(UnitOfWork.BoardGroups.GetAsync(request.Id, true));
    }
}
