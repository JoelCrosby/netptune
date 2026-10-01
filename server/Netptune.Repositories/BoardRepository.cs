using System.Text.Json;

using Dapper;

using Microsoft.EntityFrameworkCore;

using Netptune.Core.Entities;
using Netptune.Core.Meta;
using Netptune.Core.Repositories;
using Netptune.Core.Repositories.Common;
using Netptune.Core.Requests;
using Netptune.Core.ViewModels.Boards;
using Netptune.Entities.Contexts;
using Netptune.Repositories.Common;
using Netptune.Repositories.RowMaps;

using Netptune.Repositories.Sql;

namespace Netptune.Repositories;

public class BoardRepository : WorkspaceEntityRepository<DataContext, Board, int>, IBoardRepository
{
    public BoardRepository(DataContext dataContext, IDbConnectionFactory connectionFactories)
        : base(dataContext, connectionFactories)
    {
    }

    public Task<List<Board>> GetBoardsInProject(int projectId, bool isReadonly = false, bool includeGroups = false, CancellationToken cancellationToken = default, PageRequest? pageRequest = null)
    {
        pageRequest ??= new PageRequest();
        var pagination = pageRequest.GetPagination();

        var query = Entities
            .Where(board => board.ProjectId == projectId)
            .Where(board => !board.IsDeleted)
            .OrderByDescending(board => board.UpdatedAt ?? board.CreatedAt)
            .ThenByDescending(board => board.Id)
            .Skip(pagination.Skip)
            .Take(pagination.PageSize)
            .IsReadonly(isReadonly);

        if (!includeGroups) return query.ToListAsync(cancellationToken);

        return query.Include(board => board.BoardGroups).ToListAsync(cancellationToken);
    }

    public Task<List<Board>> GetBoards(string slug, bool isReadonly = false, CancellationToken cancellationToken = default)
    {
        return (from b in Entities
                join p in Context.Projects on b.ProjectId equals p.Id
                join w in Context.Workspaces on p.WorkspaceId equals w.Id
                where w.Slug == slug && !w.IsDeleted && !b.IsDeleted && !p.IsDeleted
                select b)
            .Include(x => x.Owner)
            .Include(x => x.Project)
            .AsSplitQuery()
            .ToReadonlyListAsync(isReadonly, cancellationToken);
    }

    public Task<List<Board>> GetBoardsById(IEnumerable<int> boardIds, CancellationToken cancellationToken = default)
    {
        var idList = boardIds.ToList();

        if (idList.Count == 0) return Task.FromResult(new List<Board>());

        return Entities
            .Where(board => idList.Contains(board.Id) && !board.IsDeleted)
            .Include(board => board.Project)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<List<BoardsViewModel>> GetBoardViewModels(string slug, CancellationToken cancellationToken = default, PageRequest? pageRequest = null)
    {
        pageRequest ??= new PageRequest();
        var pagination = pageRequest.GetPagination();

        using var connection = ConnectionFactory.StartConnection();

        var command = new CommandDefinition(SqlScripts.GetWorkspaceBoards, new
        {
            slug,
            skip = pagination.Skip,
            pageSize = pagination.PageSize,
        }, cancellationToken: cancellationToken);

        var rows = await connection.QueryAsync<BoardViewModelRowMap>(command);

        static BoardMeta GetMetaInfo(BoardViewModelRowMap board)
        {
            return !string.IsNullOrEmpty(board.Meta_Info)
                ? JsonSerializer.Deserialize<BoardMeta>(board.Meta_Info) ?? new()
                : new();
        }

        return rows.Select(board => new BoardViewModel
        {
            Id = board.Id,
            Name = board.Name,
            Identifier = board.Identifier,
            ProjectId = board.Project_Id,
            ProjectName = board.Project_Name,
            BoardType = board.Board_Type,
            CreatedAt = board.Created_At,
            UpdatedAt = board.Updated_At,
            MetaInfo = GetMetaInfo(board),
            OwnerUsername = $"{board.Firstname} {board.Lastname}",
            TaskCount = board.Task_Count,
            LastUpdated = board.Last_Updated ?? board.Created_At,
            Assignees = BoardViewAssigneeRowMap.ParseList(board.Assignees),
        })
            .Aggregate(new List<BoardsViewModel>(), (prev, board) =>
            {
                var last = prev.Count > 0 ? prev[^1] : null;

                if (last?.ProjectId == board.ProjectId)
                {
                    last.Boards.Add(board);

                    return prev;
                }

                if (last is null || last.ProjectId != board.ProjectId)
                {
                    prev.Add(new BoardsViewModel
                    {
                        ProjectId = board.ProjectId,
                        ProjectName = board.ProjectName,
                        Boards = new List<BoardViewModel> { board },
                    });
                }

                return prev;
            })

            .ToList();
    }

    public Task<int?> GetIdByIdentifier(string identifier, int workspaceId, CancellationToken cancellationToken = default)
    {
        return Entities
            .Where(b => b.Identifier == identifier && b.WorkspaceId == workspaceId)
            .Select(b => (int?)b.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<Board?> GetByIdentifier(string identifier, int workspaceId, bool isReadonly = false, CancellationToken cancellationToken = default)
    {
        return Entities
            .Where(b => !b.IsDeleted && b.Identifier == identifier && b.WorkspaceId == workspaceId)
            .IsReadonly(isReadonly)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<BoardViewModel?> GetViewModel(int id, bool isReadonly = false, CancellationToken cancellationToken = default)
    {
        return Entities
            .Where(board => board.Id == id && !board.IsDeleted)
            .IsReadonly(isReadonly)
            .Select(x => new BoardViewModel
            {
                Id = x.Id,
                Name = x.Name,
                Identifier = x.Identifier,
                ProjectId = x.ProjectId,
                BoardType = x.BoardType,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt,
                MetaInfo = x.MetaInfo,
                OwnerUsername = x.Owner == null
                    ? string.Empty
                    : x.Owner.DisplayName,
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<bool> Exists(string identifier, CancellationToken cancellationToken = default)
    {
        return Entities.AnyAsync(board => board.Identifier == identifier, cancellationToken);
    }

    public async Task SetBrandingFile(int boardId, int workspaceId, string metaKey, string? fileId, CancellationToken cancellationToken = default)
    {
        using var connection = ConnectionFactory.StartConnection();

        var command = new CommandDefinition(
            SqlScripts.SetBoardBrandingFile,
            new { BoardId = boardId, WorkspaceId = workspaceId, MetaKey = metaKey, FileId = fileId },
            cancellationToken: cancellationToken);

        await connection.ExecuteAsync(command);
    }
}
