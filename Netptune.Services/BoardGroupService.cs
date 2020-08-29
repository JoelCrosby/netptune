using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using MoreLinq;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Repositories;
using Netptune.Core.Requests;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Boards;
using Netptune.Core.ViewModels.Users;

namespace Netptune.Services
{
    public class BoardGroupService : IBoardGroupService
    {
        private readonly INetptuneUnitOfWork UnitOfWork;
        private readonly IIdentityService IdentityService;
        private readonly IMapper Mapper;
        private readonly IBoardGroupRepository BoardGroups;
        private readonly IBoardRepository Boards;

        public BoardGroupService(INetptuneUnitOfWork unitOfWork, IIdentityService identityService, IMapper mapper)
        {
            UnitOfWork = unitOfWork;
            IdentityService = identityService;
            Mapper = mapper;
            Boards = unitOfWork.Boards;
            BoardGroups = unitOfWork.BoardGroups;
        }

        public async Task<BoardGroupsViewModel> GetBoardGroups(string boardIdentifier, BoardGroupsFilter filter = null)
        {
            var boardId = await Boards.GetIdByIdentifier(boardIdentifier);

            if (!boardId.HasValue) return null;

            return await GetBoardGroups(boardId.Value, filter);
        }

        public async Task<BoardGroupsViewModel> GetBoardGroups(int boardId, BoardGroupsFilter filter = null)
        {
            var groups = await BoardGroups.GetBoardGroupsInBoard(boardId, true);

            foreach (var group in groups)
            {
                var tasksInGroups = group
                    .TasksInGroups
                    .OrderBy(item => item.SortOrder)
                    .ToList();

                var includeUserFilter = filter?.Users?.Any() ?? false;

                var tasks = tasksInGroups.Select(item => item.ProjectTask)
                    .Where(task => !task.IsDeleted)
                    .Where(task => !includeUserFilter || (filter?.Users.Contains(task.AssigneeId) ?? true))
                    .Select(task => task.ToViewModel());

                group.Tasks.AddRange(tasks);
            }

            var board = await UnitOfWork.Boards.GetViewModel(boardId, true);

            var userEntities = groups
                .SelectMany(group => group.TasksInGroups)
                .Select(task => task.ProjectTask)
                .Where(task => !task.IsDeleted)
                .Select(task => task.Assignee)
                .DistinctBy(user => user.UserName)
                .ToList();

            var users = Mapper.Map<List<AppUser>, List<UserViewModel>>(userEntities);

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
