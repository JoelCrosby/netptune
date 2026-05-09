using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Entities.Contexts;

namespace Netptune.SeedData.Seeders;

public sealed class BoardGroupSeeder : ISeeder
{
    public int Phase => 1;

    // One layout per board, ordered to match the project order in ProjectSeeder.
    // PAT=5, MOB=7, DSH=4, MKT=3, CLI=6, CMP=9, INF=5, MON=8
    private static readonly (string Name, BoardGroupType Type)[][] BoardGroupLayouts =
    [
        // PAT — Platform API
        [
            ("Backlog",         BoardGroupType.Backlog),
            ("Todo",            BoardGroupType.Todo),
            ("In Review",       BoardGroupType.Basic),
            ("Testing",         BoardGroupType.Basic),
            ("Done",            BoardGroupType.Done),
        ],
        // MOB — Mobile App
        [
            ("Backlog",         BoardGroupType.Backlog),
            ("Todo",            BoardGroupType.Todo),
            ("In Progress",     BoardGroupType.Basic),
            ("Code Review",     BoardGroupType.Basic),
            ("QA Testing",      BoardGroupType.Basic),
            ("Ready to Ship",   BoardGroupType.Basic),
            ("Done",            BoardGroupType.Done),
        ],
        // DSH — Dashboard
        [
            ("Backlog",         BoardGroupType.Backlog),
            ("Todo",            BoardGroupType.Todo),
            ("In Progress",     BoardGroupType.Basic),
            ("Done",            BoardGroupType.Done),
        ],
        // MKT — Marketing Site
        [
            ("Backlog",         BoardGroupType.Backlog),
            ("In Progress",     BoardGroupType.Basic),
            ("Done",            BoardGroupType.Done),
        ],
        // CLI — CLI Tools
        [
            ("Backlog",         BoardGroupType.Backlog),
            ("Todo",            BoardGroupType.Todo),
            ("In Progress",     BoardGroupType.Basic),
            ("Code Review",     BoardGroupType.Basic),
            ("Release Candidate", BoardGroupType.Basic),
            ("Done",            BoardGroupType.Done),
        ],
        // CMP — Component Library
        [
            ("Backlog",             BoardGroupType.Backlog),
            ("Scoping",             BoardGroupType.Basic),
            ("Todo",                BoardGroupType.Todo),
            ("In Progress",         BoardGroupType.Basic),
            ("Code Review",         BoardGroupType.Basic),
            ("Accessibility Review", BoardGroupType.Basic),
            ("Documentation",       BoardGroupType.Basic),
            ("Ready to Publish",    BoardGroupType.Basic),
            ("Done",                BoardGroupType.Done),
        ],
        // INF — Infrastructure
        [
            ("Backlog",         BoardGroupType.Backlog),
            ("Planning",        BoardGroupType.Basic),
            ("In Progress",     BoardGroupType.Basic),
            ("Review",          BoardGroupType.Basic),
            ("Done",            BoardGroupType.Done),
        ],
        // MON — Monitoring
        [
            ("Backlog",         BoardGroupType.Backlog),
            ("Triaged",         BoardGroupType.Basic),
            ("Todo",            BoardGroupType.Todo),
            ("In Progress",     BoardGroupType.Basic),
            ("Review",          BoardGroupType.Basic),
            ("Testing",         BoardGroupType.Basic),
            ("Ready to Deploy", BoardGroupType.Basic),
            ("Done",            BoardGroupType.Done),
        ],
    ];

    public async Task SeedAsync(DataContext dbContext, SeedContext context, CancellationToken ct)
    {
        for (var bi = 0; bi < context.Boards.Count; bi++)
        {
            var board = context.Boards[bi];
            var layout = BoardGroupLayouts[bi % BoardGroupLayouts.Length];

            for (var gi = 0; gi < layout.Length; gi++)
            {
                var (name, type) = layout[gi];

                context.BoardGroups.Add(new BoardGroup
                {
                    Name = name,
                    Type = type,
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
