using Netptune.Core;
using Netptune.Core.Encoding;
using Netptune.Core.Enums;
using Netptune.Core.Repositories;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Netptune.Services
{
    public class BoardService : IBoardService
    {
        private readonly INetptuneUnitOfWork UnitOfWork;
        private readonly IIdentityService IdentityService;
        private readonly IBoardRepository Boards;

        public BoardService(INetptuneUnitOfWork unitOfWork, IIdentityService identityService)
        {
            UnitOfWork = unitOfWork;
            IdentityService = identityService;
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
            result.Identifier = UrlSlugger.ToUrlSlug(board.Identifier);

            await UnitOfWork.CompleteAsync();

            return result;
        }

        public async Task<Board> AddBoard(Board board)
        {
            board.Identifier = UrlSlugger.ToUrlSlug(board.Identifier);

            board.BoardGroups.Add(new BoardGroup
            {
                Name = "Backlog",
                Type = BoardGroupType.Backlog,
                SortOrder = 1D
            });

            board.BoardGroups.Add(new BoardGroup
            {
                Name = "Todo",
                Type = BoardGroupType.Todo,
                SortOrder = 1.1D
            });

            board.BoardGroups.Add(new BoardGroup
            {
                Name = "Pending Review",
                Type = BoardGroupType.Basic,
                SortOrder = 1.2D
            });

            board.BoardGroups.Add(new BoardGroup
            {
                Name = "Done",
                Type = BoardGroupType.Done,
                SortOrder = 1.3D
            });

            var result = await Boards.AddAsync(board);

            await UnitOfWork.CompleteAsync();

            return result;
        }

        public async Task<Board> DeleteBoard(int id)
        {
            var board = await Boards.GetAsync(id);
            var user = await IdentityService.GetCurrentUser();

            if (board is null) return null;

            board.IsDeleted = true;
            board.DeletedByUserId = user.Id;

            await UnitOfWork.CompleteAsync();

            return board;
        }
    }
}
