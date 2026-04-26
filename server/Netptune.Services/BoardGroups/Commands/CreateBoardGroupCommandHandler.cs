using Mediator;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Boards;
using Netptune.Core.Requests;

namespace Netptune.Services.BoardGroups.Commands;

public sealed record CreateBoardGroupCommand(AddBoardGroupRequest Request) : IRequest<ClientResponse<BoardGroupViewModel>>;

public sealed class CreateBoardGroupCommandHandler : IRequestHandler<CreateBoardGroupCommand, ClientResponse<BoardGroupViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IActivityLogger Activity;

    public CreateBoardGroupCommandHandler(INetptuneUnitOfWork unitOfWork, IActivityLogger activity)
    {
        UnitOfWork = unitOfWork;
        Activity = activity;
    }

    public async ValueTask<ClientResponse<BoardGroupViewModel>> Handle(CreateBoardGroupCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;
        var boardId = req.BoardId ?? throw new ArgumentNullException(nameof(req.BoardId));

        var board = await UnitOfWork.Boards.GetAsync(boardId);

        if (board is null) return ClientResponse<BoardGroupViewModel>.NotFound;

        var sortOrder = req.SortOrder ?? await UnitOfWork.BoardGroups.GetBoardGroupDefaultSortOrder(boardId);

        var boardGroup = new BoardGroup
        {
            Name = req.Name,
            Type = req.Type ?? BoardGroupType.Basic,
            SortOrder = sortOrder,
            WorkspaceId = board.WorkspaceId,
            BoardId = board.Id,
        };

        var result = await UnitOfWork.BoardGroups.AddAsync(boardGroup);

        await UnitOfWork.CompleteAsync();

        Activity.Log(options =>
        {
            options.EntityId = result.Id;
            options.EntityType = EntityType.BoardGroup;
            options.Type = ActivityType.Create;
        });

        return ClientResponse<BoardGroupViewModel>.Success(result.ToViewModel());
    }
}
