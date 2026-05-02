using Mediator;
using Netptune.Core.Responses.Common;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Boards;

namespace Netptune.Services.Boards.Queries;

public sealed record GetBoardQuery(int Id) : IRequest<ClientResponse<BoardViewModel>>;

public sealed class GetBoardQueryHandler : IRequestHandler<GetBoardQuery, ClientResponse<BoardViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;

    public GetBoardQueryHandler(INetptuneUnitOfWork unitOfWork)
    {
        UnitOfWork = unitOfWork;
    }

    public async ValueTask<ClientResponse<BoardViewModel>> Handle(GetBoardQuery request, CancellationToken cancellationToken)
    {
        var result = await UnitOfWork.Boards.GetAsync(request.Id, true, cancellationToken);

        if (result is null) return ClientResponse<BoardViewModel>.NotFound;

        return ClientResponse<BoardViewModel>.Success(result.ToViewModel());
    }
}
