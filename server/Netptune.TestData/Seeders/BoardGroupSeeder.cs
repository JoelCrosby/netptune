using Netptune.Core.Entities;
using Netptune.Core.Enums;

namespace Netptune.TestData.Seeders;

internal static class BoardGroupSeeder
{
    internal static List<BoardGroup> Generate(List<AppUser> users, List<Board> boards) =>
        new[] { BoardGroupType.Backlog, BoardGroupType.Todo, BoardGroupType.Done }
            .SelectMany(type => boards.Select((board, i) => new BoardGroup
            {
                Name = ProjectSeeder.Names[i],
                Owner = users[i % users.Count],
                Board = board,
                Workspace = board.Workspace,
                Type = type,
            }))
            .ToList();
}
