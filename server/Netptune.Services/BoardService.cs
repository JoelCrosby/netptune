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

namespace Netptune.Services;

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

    public async Task<List<BoardViewModel>> GetBoards(int projectId)
    {
        var results = await Boards.GetBoardsInProject(projectId, true);

        return results.ConvertAll(result => result.ToViewModel());
    }

    public async Task<ClientResponse<BoardViewModel>> GetBoard(int id)
    {
        var result = await Boards.GetAsync(id, true);

        if (result is null)
        {
            return ClientResponse<BoardViewModel>.Failed();
        }

        return ClientResponse<BoardViewModel>.Success(result.ToViewModel());
    }

    public async Task<ClientResponse<BoardView>> GetBoardView(string boardIdentifier, BoardGroupsFilter? filter = null)
    {
        var workspaceId = await IdentityService.GetWorkspaceId();
        var nullableBoardId = await Boards.GetIdByIdentifier(boardIdentifier, workspaceId);

        if (!nullableBoardId.HasValue)
        {
            return ClientResponse<BoardView>.NotFound;
        }

        var boardId = nullableBoardId.Value;

        var groups = await UnitOfWork.BoardGroups.GetBoardView(boardId, filter?.Term);
        var board = await UnitOfWork.Boards.GetViewModel(boardId, true);

        if (groups is null || board is null)
        {
            return ClientResponse<BoardView>.Failed();
        }

        var includeUserFilter = filter?.Users.Any() ?? false;
        var includeTagFilter = filter?.Tags.Any() ?? false;
        var includeFlaggedFilter = filter?.Flagged ?? false;

        var userIds = groups
            .SelectMany(group => group.Tasks)
            .SelectMany(task => task.Assignees)
                .Select(rel => rel.Id)
            .Distinct()
            .ToList();

        foreach (var group in groups)
        {
            group.Tasks = group.Tasks
                .Where(task => !includeUserFilter || (filter?.Users.Any(u => task.Assignees.Select(a => a.Id).Contains(u)) ?? true))
                .Where(task => !includeFlaggedFilter || task.IsFlagged)
                .Where(task => !includeTagFilter || (filter?.Tags.Intersect(task.Tags).Any() ?? true))
                .ToList();
        }

        var userEntities = await UnitOfWork.Users.GetAllByIdAsync(userIds, true);
        var users = userEntities.Select(user => user.ToViewModel());

        var result = new BoardView
        {
            Groups = groups,
            Board = board,
            Users = users,
        };

        return ClientResponse<BoardView>.Success(result);
    }

    public async Task<ClientResponse<BoardViewModel>> UpdateBoard(UpdateBoardRequest request)
    {
        if (!request.Id.HasValue)
        {
            throw new Exception($"{nameof(request.Id)} is required");
        }

        var result = await Boards.GetAsync(request.Id.Value);

        if (result is null)
        {
            return ClientResponse<BoardViewModel>.NotFound;
        }

        result.Name = request.Name ?? result.Name;
        result.Identifier = request.Identifier?.ToUrlSlug() ?? result.Identifier;

        await UnitOfWork.CompleteAsync();

        var payload = result.ToViewModel();

        return ClientResponse<BoardViewModel>.Success(payload);
    }

    public async Task<ClientResponse<BoardViewModel>> AddBoard(AddBoardRequest request)
    {
        if (!request.ProjectId.HasValue)
        {
            throw new Exception($"{nameof(request.ProjectId)} is required");
        }

        var project = await UnitOfWork.Projects.GetAsync(request.ProjectId.Value, true);

        if (project is null)
        {
            return ClientResponse<BoardViewModel>.NotFound;
        }

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
        var userId = IdentityService.GetCurrentUserId();

        if (board is null) return ClientResponse.NotFound;

        board.Delete(userId);

        await UnitOfWork.CompleteAsync();

        return ClientResponse.Success();
    }

    public async Task<List<BoardsViewModel>?> GetBoardsInWorkspace()
    {
        var workspaceId = IdentityService.GetWorkspaceKey();
        var workspaceExists = await UnitOfWork.Workspaces.Exists(workspaceId);

        if (!workspaceExists) return null;

        return await Boards.GetBoardViewModels(workspaceId);
    }

    public async Task<List<BoardViewModel>?> GetBoardsInProject(int projectId)
    {
        var results = await Boards.GetBoardsInProject(projectId, true);

        return results.ConvertAll(result => result.ToViewModel());
    }

    public async Task<ClientResponse<IsSlugUniqueResponse>> IsIdentifierUnique(string identifier)
    {
        var slugLower = identifier.ToUrlSlug();
        var exists = await Boards.Exists(slugLower);

        return ClientResponse<IsSlugUniqueResponse>.Success(new IsSlugUniqueResponse
        {
            Request = identifier,
            Slug = slugLower,
            IsUnique = !exists,
        });
    }
}
