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

namespace Netptune.Services
{
    public class BoardGroupService : ServiceBase<BoardGroup>, IBoardGroupService
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

        public async Task<ClientResponse<BoardGroup>> UpdateBoardGroup(BoardGroup boardGroup)
        {
            var result = await BoardGroups.GetAsync(boardGroup.Id);

            if (result is null) return null;

            result.Name = boardGroup.Name;
            result.SortOrder = boardGroup.SortOrder;

            await UnitOfWork.CompleteAsync();

            return Success(result);
        }

        public async Task<ClientResponse<BoardGroup>> AddBoardGroup(AddBoardGroupRequest request)
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

            return Success(boardGroup);
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
}
