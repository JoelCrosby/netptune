using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Entities.Contexts;

namespace Netptune.SeedData.Seeders;

public sealed class BoardGroupSeeder : ISeeder
{
    public int Phase => 1;

    private static readonly BoardGroupType[] GroupTypes =
    [
        BoardGroupType.Backlog,
        BoardGroupType.Todo,
        BoardGroupType.Done,
    ];

    public async Task SeedAsync(DataContext dbContext, SeedContext context, CancellationToken ct)
    {
        context.BoardGroups.AddRange(
            GroupTypes.SelectMany((type, ti) => context.Boards.Select((board, bi) => new BoardGroup
            {
                Name = type.ToString(),
                Type = type,
                SortOrder = ti,
                Board = board,
                Owner = context.Users[bi % context.Users.Count],
                Workspace = board.Workspace,
            }))
        );

        await dbContext.BoardGroups.AddRangeAsync(context.BoardGroups, ct);
    }
}
