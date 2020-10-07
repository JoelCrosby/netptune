using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Netptune.Core.Encoding;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Repositories;
using Netptune.Core.Requests;
using Netptune.Core.Responses;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Boards;

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
            return Boards.GetBoardsInProject(projectId, true);
        }

        public Task<Board> GetBoard(int id)
        {
            return Boards.GetAsync(id, true);
        }

        public async Task<BoardView> GetBoardView(string boardIdentifier, BoardGroupsFilter filter = null)
        {
            var nullableBoardId = await Boards.GetIdByIdentifier(boardIdentifier);

            if (!nullableBoardId.HasValue) return null;

            var boardId = nullableBoardId.Value;

            var groups = await UnitOfWork.BoardGroups.GetBoardView(boardId);
            var board = await UnitOfWork.Boards.GetViewModel(boardId, true);

            var includeUserFilter = filter?.Users?.Any() ?? false;
            var includeTagFilter = filter?.Tags?.Any() ?? false;
            var includeFlaggedFilter = filter?.Flagged ?? false;

            foreach (var group in groups)
            {
                group.Tasks = group.Tasks
                    .Where(task => !includeUserFilter || (filter?.Users.Contains(task.AssigneeId) ?? true))
                    .Where(task => !includeFlaggedFilter || task.IsFlagged)
                    .Where(task => !includeTagFilter || (filter?.Tags.Intersect(task.Tags).Any() ?? true))
                    .ToList();
            }

            var userIds = groups
                .SelectMany(group => group.Tasks)
                .Select(task => task.AssigneeId)
                .Distinct();

            var userEntities = await UnitOfWork.Users.GetAllByIdAsync(userIds, true);
            var users = userEntities.Select(user => user.ToViewModel());

            return new BoardView
            {
                Groups = groups,
                Board = board,
                Users = users,
            };
        }

        public async Task<ClientResponse<BoardViewModel>> UpdateBoard(Board board)
        {
            var result = await Boards.GetAsync(board.Id);

            if (result is null) return null;

            result.Name = board.Name;
            result.Identifier = board.Identifier.ToUrlSlug();

            await UnitOfWork.CompleteAsync();

            var payload = result.ToViewModel();

            return ClientResponse<BoardViewModel>.Success(payload);
        }

        public async Task<ClientResponse<BoardViewModel>> AddBoard(AddBoardRequest request)
        {
            if (!request.ProjectId.HasValue)
            {
                throw new Exception("ProjectId is required");
            }

            var project = await UnitOfWork.Projects.GetAsync(request.ProjectId.Value);
            var workspaceId = project.WorkspaceId;

            var board = new Board
            {
                Name = request.Name,
                Identifier = request.Identifier.ToUrlSlug(),
                ProjectId = request.ProjectId.Value,
                MetaInfo = request.Meta,
                WorkspaceId = workspaceId,
            };


            board.BoardGroups.Add(new BoardGroup
            {
                Name = "Backlog",
                Type = BoardGroupType.Backlog,
                SortOrder = 1D,
                WorkspaceId = workspaceId,
            });

            board.BoardGroups.Add(new BoardGroup
            {
                Name = "Todo",
                Type = BoardGroupType.Todo,
                SortOrder = 1.1D,
                WorkspaceId = workspaceId,
            });

            board.BoardGroups.Add(new BoardGroup
            {
                Name = "Done",
                Type = BoardGroupType.Done,
                SortOrder = 1.2D,
                WorkspaceId = workspaceId,
            });

            var result = await Boards.AddAsync(board);

            await UnitOfWork.CompleteAsync();

            var payload = result.ToViewModel();

            return ClientResponse<BoardViewModel>.Success(payload);
        }

        public async Task<ClientResponse> Delete(int id)
        {
            var board = await Boards.GetAsync(id);
            var userId = await IdentityService.GetCurrentUserId();

            if (board is null || userId is null) return null;

            board.Delete(userId);

            await UnitOfWork.CompleteAsync();

            return ClientResponse.Success();
        }

        public async Task<List<BoardViewModel>> GetBoardsInWorkspace(string slug)
        {
            var workspaceExists = await UnitOfWork.Workspaces.Exists(slug);

            if (!workspaceExists) return null;

            var results = await Boards.GetBoards(slug, true);

            return results.Select(result => result.ToViewModel()).ToList();
        }

        public async Task<ClientResponse<IsSlugUniqueResponse>> IsIdentifierUnique(string identifier)
        {
            var slugLower = identifier.ToUrlSlug();
            var exists = await Boards.Exists(slugLower);

            return ClientResponse<IsSlugUniqueResponse>.Success(new IsSlugUniqueResponse
            {
                Request = identifier,
                Slug = slugLower,
                IsUnique = !exists
            });
        }
    }
}
