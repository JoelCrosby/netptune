using System;
using System.Threading.Tasks;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Repositories;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Common;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Boards;

namespace Netptune.Services;

public class BoardGroupService : ServiceBase<BoardGroupViewModel>, IBoardGroupService
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService IdentityService;
    private readonly IBoardGroupRepository BoardGroups;
    private readonly IBoardRepository Boards;

    public BoardGroupService(INetptuneUnitOfWork unitOfWork, IIdentityService identityService)
    {
        UnitOfWork = unitOfWork;
        IdentityService = identityService;
        Boards = unitOfWork.Boards;
        BoardGroups = unitOfWork.BoardGroups;
    }

    public Task<BoardGroup> GetBoardGroup(int id)
    {
        return BoardGroups.GetAsync(id, true);
    }

    public async Task<ClientResponse<BoardGroupViewModel>> UpdateBoardGroup(UpdateBoardGroupRequest request)
    {
        var result = await BoardGroups.GetAsync(request.BoardGroupId!.Value);

        if (result is null) return null;

        if (request.Name is { })
        {
            result.Name = request.Name;
        }

        if (request.SortOrder.HasValue)
        {
            result.SortOrder = request.SortOrder.Value;
        }

        await UnitOfWork.CompleteAsync();

        return Success(result.ToViewModel());
    }

    public async Task<ClientResponse<BoardGroupViewModel>> AddBoardGroup(AddBoardGroupRequest request)
    {
        var boardId = request.BoardId ?? throw new ArgumentNullException(nameof(request.BoardId));

        var board = await Boards.GetAsync(boardId);

        if (board is null) return null;

        var sortOrder = request.SortOrder ?? await BoardGroups.GetBoardGroupDefaultSortOrder(boardId);

        var boardGroup = new BoardGroup
        {
            Name = request.Name,
            Type = request.Type ?? BoardGroupType.Basic,
            SortOrder = sortOrder,
            WorkspaceId = board.WorkspaceId,
        };

        board.BoardGroups.Add(boardGroup);

        await UnitOfWork.CompleteAsync();

        return Success(boardGroup.ToViewModel());
    }

    public async Task<ClientResponse> Delete(int id)
    {
        var boardGroup = await BoardGroups.GetAsync(id);
        var userId = await IdentityService.GetCurrentUserId();

        if (boardGroup is null || userId is null) return null;

        boardGroup.Delete(userId);

        await UnitOfWork.CompleteAsync();

        return ClientResponse.Success();
    }
}
