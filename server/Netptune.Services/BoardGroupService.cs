using System;
using System.Linq;
using System.Threading.Tasks;

using MoreLinq;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Repositories;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Boards;

namespace Netptune.Services
{
    public class BoardGroupService : IBoardGroupService
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

        public async Task<BoardGroupsViewModel> GetBoardGroups(string boardIdentifier, BoardGroupsFilter filter = null)
        {
            var nullableBoardId = await Boards.GetIdByIdentifier(boardIdentifier);

            if (!nullableBoardId.HasValue) return null;

            var boardId = nullableBoardId.Value;
            var groups = await BoardGroups.GetBoardGroupsInBoard(boardId, true);

            var includeUserFilter = filter?.Users?.Any() ?? false;
            var includeTagFilter = filter?.Tags?.Any() ?? false;
            var includeFlaggedFilter = filter?.Flagged ?? false;

            foreach (var group in groups)
            {
                var tasksInGroups = group
                    .TasksInGroups
                    .OrderBy(item => item.SortOrder);

                var tasks = tasksInGroups.Select(item => item.ProjectTask)
                    .Where(task => !task.IsDeleted)
                    .Where(task => !includeUserFilter || (filter?.Users.Contains(task.AssigneeId) ?? true))
                    .Where(task => !includeFlaggedFilter || task.IsFlagged)
                    .Select(task => task.ToViewModel())
                    .Where(task => !includeTagFilter || (filter?.Tags.Intersect(task.Tags).Any() ?? true));

                group.Tasks.AddRange(tasks);
            }

            var board = await UnitOfWork.Boards.GetViewModel(boardId, true);

            var users = groups
                .SelectMany(group => group.TasksInGroups)
                .Select(task => task.ProjectTask)
                .Where(task => !task.IsDeleted)
                .Select(task => task.Assignee)
                .DistinctBy(user => user.UserName)
                .Select(user => user.ToViewModel());

            return new BoardGroupsViewModel
            {
                Groups = groups,
                Board = board,
                Users = users,
            };
        }

        public Task<BoardGroup> GetBoardGroup(int id)
        {
            return BoardGroups.GetAsync(id, true);
        }

        public async Task<BoardGroup> UpdateBoardGroup(BoardGroup boardGroup)
        {
            var result = await BoardGroups.GetAsync(boardGroup.Id);

            if (result is null) return null;

            result.Name = boardGroup.Name;
            result.SortOrder = boardGroup.SortOrder;

            await UnitOfWork.CompleteAsync();

            return result;
        }


        public async Task<BoardGroup> AddBoardGroup(AddBoardGroupRequest request)
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

            return boardGroup;
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
