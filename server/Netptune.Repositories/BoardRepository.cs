using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

using Dapper;

using Microsoft.EntityFrameworkCore;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Meta;
using Netptune.Core.Repositories;
using Netptune.Core.Repositories.Common;
using Netptune.Core.ViewModels.Boards;
using Netptune.Entities.Contexts;
using Netptune.Repositories.Common;
using Netptune.Repositories.RowMaps;

namespace Netptune.Repositories;

public class BoardRepository : WorkspaceEntityRepository<DataContext, Board, int>, IBoardRepository
{
    public BoardRepository(DataContext dataContext, IDbConnectionFactory connectionFactories)
        : base(dataContext, connectionFactories)
    {
    }

    public Task<List<Board>> GetBoardsInProject(int projectId, bool isReadonly = false, bool includeGroups = false)
    {
        var query = Entities
            .Where(board => board.ProjectId == projectId)
            .Where(board => !board.IsDeleted)
            .IsReadonly(isReadonly);

        if (!includeGroups) return query.ToListAsync();

        return query.Include(board => board.BoardGroups).ToListAsync();
    }

    public Task<Board?> GetDefaultBoardInProject(int projectId, bool isReadonly = false, bool includeGroups = false)
    {
        var query = Entities
            .Where(board => board.ProjectId == projectId)
            .Where(board => !board.IsDeleted)
            .Where(board => board.BoardType == BoardType.Default)
            .IsReadonly(isReadonly);

        if (!includeGroups) return query.FirstOrDefaultAsync();

        return query
            .Include(board => board.BoardGroups)
            .ThenInclude(group => group.TasksInGroups)
            .FirstOrDefaultAsync();
    }

    public Task<List<Board>> GetBoards(string slug, bool isReadonly = false)
    {
        return (from b in Entities
                join p in Context.Projects on b.ProjectId equals p.Id
                join w in Context.Workspaces on p.WorkspaceId equals w.Id
                where w.Slug == slug && !w.IsDeleted && !b.IsDeleted && !p.IsDeleted
                select b)
            .Include(x => x.Owner)
            .Include(x => x.Project)
            .ToReadonlyListAsync(isReadonly);
    }

    public async Task<List<BoardsViewModel>> GetBoardViewModels(string slug)
    {
        using var connection = ConnectionFactory.StartConnection();

        var results = await connection.QueryMultipleAsync(@"
                SELECT b.id,
                       b.name,
                       b.identifier,
                       b.project_id,
                       b.board_type,
                       CAST(b.created_at AS timestamp with time zone),
                       CAST(b.updated_at AS timestamp with time zone),
                       b.meta_info,
                       (u.id IS NULL),
                       u.firstname,
                       u.lastname,
                       p.name AS project_name
                FROM boards AS b
                         INNER JOIN projects AS p ON b.project_id = p.id AND NOT p.is_deleted
                         INNER JOIN workspaces AS w ON p.workspace_id = w.id AND NOT w.is_deleted
                         LEFT JOIN users AS u ON b.owner_id = u.id
                WHERE w.slug = @slug AND NOT b.is_deleted
                ORDER BY p.updated_at, b.updated_at
            ", new { slug });

        var rows = results.Read<BoardViewModelRowMap>();

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
                MetaInfo = JsonSerializer.Deserialize<BoardMeta>(board.Meta_Info) ?? new BoardMeta(),
                OwnerUsername = $"{board.Firstname} {board.Lastname}",
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

    public async Task<int?> GetIdByIdentifier(string identifier, int workspaceId)
    {
        var result = await
            (from b in Entities
                where b.Identifier == identifier && b.WorkspaceId == workspaceId
                select b.Id)
            .ToListAsync();

        if (result.Any()) return result.FirstOrDefault();

        return null;
    }

    public Task<Board?> GetByIdentifier(string identifier, int workspaceId, bool isReadonly = false)
    {
        return Entities
            .Where(b => !b.IsDeleted && b.Identifier == identifier && b.WorkspaceId == workspaceId)
            .IsReadonly(isReadonly)
            .FirstOrDefaultAsync();
    }

    public async Task<BoardViewModel?> GetViewModel(int id, bool isReadonly = false)
    {
        var result = await Entities
            .IsReadonly(isReadonly)
            .Include(board => board.Project)
            .FirstOrDefaultAsync(board => board.Id == id && !board.IsDeleted);

        return result?.ToViewModel();
    }

    public Task<bool> Exists(string identifier)
    {
        return Entities.AnyAsync(board => board.Identifier == identifier);
    }
}
