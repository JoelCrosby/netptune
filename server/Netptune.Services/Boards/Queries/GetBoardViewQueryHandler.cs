using Mediator;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Boards;

namespace Netptune.Services.Boards.Queries;

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
        var nullableBoardId = await UnitOfWork.Boards.GetIdByIdentifier(request.Identifier, workspaceId);

        if (!nullableBoardId.HasValue) return ClientResponse<BoardView>.NotFound;

        var boardId = nullableBoardId.Value;

        var groups = await UnitOfWork.BoardGroups.GetBoardViewGroups(boardId, request.Filter?.Term);
        var board = await UnitOfWork.Boards.GetViewModel(boardId, true);

        if (groups is null || board is null) return ClientResponse<BoardView>.Failed();

        var includeUserFilter = request.Filter?.Users.Any() ?? false;
        var includeTagFilter = request.Filter?.Tags.Any() ?? false;

        var userIds = groups
            .SelectMany(group => group.Tasks)
            .SelectMany(task => task.Assignees)
            .Select(rel => rel.Id)
            .ToHashSet();

        foreach (var group in groups)
        {
            group.Tasks = group.Tasks.Where(task =>
            {
                var matchUser = !includeUserFilter || (request.Filter?.Users.Any(u => task.Assignees.Any(a => a.Id == u)) ?? true);
                if (!matchUser) return false;

                var matchTag = !includeTagFilter || (request.Filter?.Tags.Intersect(task.Tags).Any() ?? true);
                return matchTag;
            }).ToList();
        }

        var userEntities = await UnitOfWork.Users.GetAllByIdAsync(userIds, true);
        var users = userEntities.Select(user => user.ToViewModel());

        return new BoardView
        {
            Groups = groups,
            Board = board,
            Users = users,
        };
    }
}
