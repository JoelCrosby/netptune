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

    public Task<BoardGroup?> GetBoardGroup(int id)
    {
        return BoardGroups.GetAsync(id, true);
    }

    public async Task<ClientResponse<BoardGroupViewModel>> Update(UpdateBoardGroupRequest request)
    {
        var result = await BoardGroups.GetAsync(request.BoardGroupId!.Value);

        if (result is null)
        {
            return ClientResponse<BoardGroupViewModel>.NotFound;
        }

        result.Name = request.Name ?? result.Name;
        result.SortOrder = request.SortOrder ?? result.SortOrder;

        await UnitOfWork.CompleteAsync();

        return Success(result.ToViewModel());
    }

    public async Task<ClientResponse<BoardGroupViewModel>> Create(AddBoardGroupRequest request)
    {
        var boardId = request.BoardId ?? throw new ArgumentNullException(nameof(request.BoardId));

        var board = await Boards.GetAsync(boardId);

        if (board is null)
        {
            return ClientResponse<BoardGroupViewModel>.NotFound;
        }

        var sortOrder = request.SortOrder ?? await BoardGroups.GetBoardGroupDefaultSortOrder(boardId);

        var boardGroup = new BoardGroup
        {
            Name = request.Name,
            Type = request.Type ?? BoardGroupType.Basic,
            SortOrder = sortOrder,
            WorkspaceId = board.WorkspaceId,
            BoardId = board.Id,
        };

        var result = await UnitOfWork.BoardGroups.AddAsync(boardGroup);

        await UnitOfWork.CompleteAsync();

        return Success(result.ToViewModel());
    }

    public async Task<ClientResponse> Delete(int id)
    {
        var boardGroup = await BoardGroups.GetAsync(id);
        var userId = IdentityService.GetCurrentUserId();

        if (boardGroup is null) return ClientResponse.NotFound;

        boardGroup.Delete(userId);

        await UnitOfWork.CompleteAsync();

        return ClientResponse.Success();
    }
}
