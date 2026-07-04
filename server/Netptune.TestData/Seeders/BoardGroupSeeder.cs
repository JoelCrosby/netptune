using Netptune.Core.Entities;

namespace Netptune.TestData.Seeders;

internal static class BoardGroupSeeder
{
    internal static List<BoardGroup> Generate(List<AppUser> users, List<Board> boards) =>
        new[] { "Backlog", "Todo", "Done" }
            .SelectMany(name => boards.Select((board, i) => new BoardGroup
            {
                Name = name,
                Owner = users[i % users.Count],
                Board = board,
                Workspace = board.Workspace,
            }))
            .ToList();
}
