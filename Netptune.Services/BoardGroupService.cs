using System;
using System.Linq;
using System.Threading.Tasks;

using MoreLinq;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Repositories;
using Netptune.Core.Requests;
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

        public async Task<BoardGroupsViewModel> GetBoardGroups(string boardIdentifier)
        {
            var boardId = await Boards.GetIdByIndentifier(boardIdentifier);

            if (!boardId.HasValue) return null;

            return await GetBoardGroups(boardId.Value);
        }

        public async Task<BoardGroupsViewModel> GetBoardGroups(int boardId)
        {
            var groups = await BoardGroups.GetBoardGroupsInBoard(boardId, true);

            foreach (var group in groups)
            {
                var tasksInGroups = group
                    .TasksInGroups
                    .Where(item => !item.IsDeleted)
                    .OrderBy(item => item.SortOrder)
                    .ToList();

                var tasks = tasksInGroups.Select(item => item.ProjectTask)
                    .Where(task => !task.IsDeleted)
                    .Select(task => task.ToViewModel());

                group.Tasks.AddRange(tasks);
            }

            var board = await UnitOfWork.Boards.GetViewModel(boardId);

            var users = groups
                .SelectMany(group => group.TasksInGroups)
                .Where(group => !group.IsDeleted)
                .Select(task => task.ProjectTask)
                .Where(task => !task.IsDeleted)
                .Select(task => task.Assignee)
                .DistinctBy(user => user.UserName)
                .ToList();

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
            };

            board.BoardGroups.Add(boardGroup);

            await UnitOfWork.CompleteAsync();

            return boardGroup;
        }

        public async Task<BoardGroup> DeleteBoardGroup(int id)
        {
            var boardGroup = await BoardGroups.GetAsync(id);
            var user = await IdentityService.GetCurrentUser();

            if (boardGroup is null || user is null) return null;

            boardGroup.IsDeleted = true;
            boardGroup.DeletedByUserId = user.Id;

            await UnitOfWork.CompleteAsync();

            return boardGroup;
        }
    }
}
