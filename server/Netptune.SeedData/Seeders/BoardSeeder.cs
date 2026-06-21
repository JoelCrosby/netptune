using Netptune.Core.Encoding;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Meta;
using Netptune.Entities.Contexts;

namespace Netptune.SeedData.Seeders;

public sealed class BoardSeeder : ISeeder
{
    public int Phase => 1;

    private const string DefaultColor = "#3b82f6";

    public async Task SeedAsync(DataContext dbContext, SeedContext context, CancellationToken ct)
    {
        context.Boards.AddRange(context.Projects.Select((project, i) => new Board
        {
            Name = project.Name,
            Identifier = project.Name.ToUrlSlug(),
            BoardType = BoardType.Default,
            MetaInfo = new BoardMeta
            {
                Color = project.MetaInfo?.Color
                    ?? project.Workspace?.MetaInfo?.Color
                    ?? DefaultColor,
            },
            Owner = context.Users[i % context.Users.Count],
            Project = project,
            Workspace = project.Workspace!,
        }));

        await dbContext.Boards.AddRangeAsync(context.Boards, ct);
    }
}
