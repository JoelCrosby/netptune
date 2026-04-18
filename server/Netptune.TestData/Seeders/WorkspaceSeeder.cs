using Netptune.Core.Encoding;
using Netptune.Core.Entities;

namespace Netptune.TestData.Seeders;

internal static class WorkspaceSeeder
{
    private static readonly string[] Names = ["Netptune", "Linux"];

    private static readonly string[] Descriptions =
    [
        "Synergize innovative paradigms",
        "Harness open source synergies",
    ];

    private static readonly string[] Colors = ["#3b82f6", "#10b981"];

    internal static List<Workspace> Generate(List<AppUser> users) =>
        Names.Select((name, i) => new Workspace
        {
            Name = name,
            Description = Descriptions[i],
            Slug = name.ToUrlSlug(),
            MetaInfo = new() { Color = Colors[i] },
            Owner = users[i % users.Count],
        }).ToList();
}
