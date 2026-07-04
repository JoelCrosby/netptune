using Netptune.Core.Entities;
using Netptune.Entities.Contexts;

namespace Netptune.SeedData.Seeders;

public sealed class BoardGroupSeeder : ISeeder
{
    public int Phase => 1;

    // One layout per board, ordered to match the project order in ProjectSeeder.
    // PAT=5, MOB=7, DSH=4, MKT=3, CLI=6, CMP=9, INF=5, MON=8
    private static readonly string[][] BoardGroupLayouts =
    [
        // PAT — Platform API
        ["Backlog", "Todo", "In Review", "Testing", "Done"],
        // MOB — Mobile App
        ["Backlog", "Todo", "In Progress", "Code Review", "QA Testing", "Ready to Ship", "Done"],
        // DSH — Dashboard
        ["Backlog", "Todo", "In Progress", "Done"],
        // MKT — Marketing Site
        ["Backlog", "In Progress", "Done"],
        // CLI — CLI Tools
        ["Backlog", "Todo", "In Progress", "Code Review", "Release Candidate", "Done"],
        // CMP — Component Library
        ["Backlog", "Scoping", "Todo", "In Progress", "Code Review", "Accessibility Review", "Documentation", "Ready to Publish", "Done"],
        // INF — Infrastructure
        ["Backlog", "Planning", "In Progress", "Review", "Done"],
        // MON — Monitoring
        ["Backlog", "Triaged", "Todo", "In Progress", "Review", "Testing", "Ready to Deploy", "Done"],
    ];

    public async Task SeedAsync(DataContext dbContext, SeedContext context, CancellationToken ct)
    {
        for (var bi = 0; bi < context.Boards.Count; bi++)
        {
            var board = context.Boards[bi];
            var layout = BoardGroupLayouts[bi % BoardGroupLayouts.Length];

            for (var gi = 0; gi < layout.Length; gi++)
            {
                context.BoardGroups.Add(new BoardGroup
                {
                    Name = layout[gi],
                    SortOrder = gi,
                    Board = board,
                    Owner = context.Users[bi % context.Users.Count],
                    Workspace = board.Workspace,
                });
            }
        }

        await dbContext.BoardGroups.AddRangeAsync(context.BoardGroups, ct);
    }
}
