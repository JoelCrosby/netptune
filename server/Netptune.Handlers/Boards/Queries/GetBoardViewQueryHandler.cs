using Mediator;

using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Boards;

namespace Netptune.Handlers.Boards.Queries;

public sealed record GetBoardViewQuery(string Identifier, BoardGroupsFilter? Filter) : IRequest<ClientResponse<BoardView>>;

public sealed class GetBoardViewQueryHandler : IRequestHandler<GetBoardViewQuery, ClientResponse<BoardView>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public GetBoardViewQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<ClientResponse<BoardView>> Handle(GetBoardViewQuery request, CancellationToken cancellationToken)
    {
        var workspaceId = await Identity.GetWorkspaceId();
        var nullableBoardId = await UnitOfWork.Boards.GetIdByIdentifier(request.Identifier, workspaceId, cancellationToken);

        if (!nullableBoardId.HasValue) return ClientResponse<BoardView>.NotFound;

        var boardId = nullableBoardId.Value;

        var currentUserId = Identity.TryGetCurrentUserId();
        var groups = await UnitOfWork.BoardGroups.GetBoardViewGroups(
            boardId,
            currentUserId,
            request.Filter,
            cancellationToken);
        var board = await UnitOfWork.Boards.GetViewModel(boardId, true, cancellationToken);

        if (groups is null || board is null) return ClientResponse<BoardView>.Failed();

        var userIds = groups
            .SelectMany(group => group.Tasks)
            .SelectMany(task => task.Assignees)
            .Select(rel => rel.Id)
            .ToHashSet();

        var userEntities = await UnitOfWork.Users.GetAllByIdAsync(userIds, true, cancellationToken);
        var users = userEntities.Select(user => user.ToViewModel());

        return new BoardView
        {
            Groups = groups,
            Board = board,
            Users = users,
        };
    }
}
