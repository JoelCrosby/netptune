using System.Collections.Generic;
using System.Threading.Tasks;

using Netptune.Core.Repositories;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Models;

namespace Netptune.Services
{
    public class BoardGroupService : IBoardGroupService
    {
        protected readonly INetptuneUnitOfWork UnitOfWork;
        protected readonly IBoardGroupRepository BoardGroups;
        protected readonly IBoardRepository Boards;

        public BoardGroupService(INetptuneUnitOfWork unitOfWork)
        {
            UnitOfWork = unitOfWork;
            Boards = unitOfWork.Boards;
            BoardGroups = unitOfWork.BoardGroups;
        }

        public Task<List<BoardGroup>> GetBoardGroups(int boardId)
        {
            return BoardGroups.GetBoardGroupsInBoard(boardId);
        }

        public Task<BoardGroup> GetBoardGroup(int id)
        {
            return BoardGroups.GetAsync(id);
        }

        public async Task<BoardGroup> UpdateBoardGroup(BoardGroup boardGroup)
        {
            var result = await BoardGroups.GetAsync(boardGroup.Id);

            if (result is null) return null;

            result.Name = boardGroup.Name;

            await UnitOfWork.CompleteAsync();

            return result;
        }
    

        public async Task<BoardGroup> AddBoardGroup(BoardGroup boardGroup)
        {
            var board = await Boards.GetAsync(boardGroup.BoardId);

            if (board is null) return null;

            board.BoardGroups.Add(boardGroup);

            await UnitOfWork.CompleteAsync();

            return boardGroup;
        }

        public async Task<BoardGroup> DeleteBoardGroup(int id, AppUser user)
        {
            var boardGroup = await BoardGroups.GetAsync(id);

            if (boardGroup is null) return null;

            boardGroup.IsDeleted = true;
            boardGroup.DeletedByUserId = user.Id;

            await UnitOfWork.CompleteAsync();

            return boardGroup;
        }
    }
}
