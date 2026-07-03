using Netptune.Core.Encoding;
using Netptune.Core.Entities;
using Netptune.Core.Enums;

namespace Netptune.TestData.Seeders;

internal static class BoardSeeder
{
    private const string DefaultColor = "#3b82f6";

    internal static List<Board> Generate(List<AppUser> users, List<Project> projects) =>
        ProjectSeeder.Names.Select((name, i) => new Board
        {
            Name = name,
            Identifier = name.ToUrlSlug(),
            BoardType = BoardType.Default,
            MetaInfo = new()
            {
                Color = projects[i].MetaInfo?.Color
                    ?? projects[i].Workspace.MetaInfo?.Color
                    ?? DefaultColor,
            },
            Owner = users[i % users.Count],
            Project = projects[i],
            Workspace = projects[i].Workspace,
        }).ToList();
}
