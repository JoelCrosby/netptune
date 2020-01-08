
using System.Collections.Generic;
using System.Threading.Tasks;
using Netptune.Core.Extensions;
using Netptune.Core.Repositories;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Models;

namespace Netptune.Services
{
    public class BoardService : IBoardService
    {
        protected readonly INetptuneUnitOfWork UnitOfWork;
        protected readonly IBoardRepository Boards;

        public BoardService(INetptuneUnitOfWork unitOfWork)
        {
            UnitOfWork = unitOfWork;
            Boards = unitOfWork.Boards;
        }

        public Task<List<Board>> GetBoards(int projectId)
        {
            return Boards.GetBoardsInProject(projectId);
        }

        public Task<Board> GetBoard(int id)
        {
            return Boards.GetAsync(id);
        }

        public async Task<Board> UpdateBoard(Board board)
        {
            var result = await Boards.GetAsync(board.Id);

            if (result is null) return null;

            result.Name = board.Name;
            result.Identifier = board.Identifier.ToUrlSlug();

            await UnitOfWork.CompleteAsync();

            return result;
        }

        public async Task<Board> AddBoard(Board board)
        {
            board.Identifier = board.Identifier.ToUrlSlug();

            var result = await Boards.AddAsync(board);

            await UnitOfWork.CompleteAsync();

            return result;
        }

        public async Task<Board> DeleteBoard(int id, AppUser user)
        {
            var board = await Boards.GetAsync(id);

            if (board is null) return null;

            board.IsDeleted = true;
            board.DeletedByUserId = user.Id;

            await UnitOfWork.CompleteAsync();

            return board;
        }
    }
}
