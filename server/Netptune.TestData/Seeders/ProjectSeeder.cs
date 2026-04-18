using Netptune.Core.Encoding;
using Netptune.Core.Entities;

namespace Netptune.TestData.Seeders;

internal static class ProjectSeeder
{
    internal static readonly string[] Names = ["NeoVim", "VsCode", "Emacs", "Kakoune"];

    private static readonly string[] Descriptions =
    [
        "Leverage extensible text editing workflows",
        "Orchestrate developer productivity at scale",
        "Maximize lisp-based configuration power",
        "Streamline modal editing experience",
    ];

    internal static List<Project> Generate(List<AppUser> users, List<Workspace> workspaces) =>
        Names.Select((name, i) => new Project
        {
            Name = name,
            Description = Descriptions[i],
            Key = name.ToUrlSlug()[..3],
            MetaInfo = new(),
            Owner = users[i % users.Count],
            Workspace = workspaces[i % workspaces.Count],
        }).ToList();
}
