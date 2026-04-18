using Netptune.Core.Entities;

namespace Netptune.TestData.Seeders;

internal static class TagSeeder
{
    private static readonly string[] Names =
    [
        "Typescript", "Python", "Go", "Java", "Kotlin", "C#", "Swift",
    ];

    internal static List<Tag> Generate(List<AppUser> users, List<ProjectTask> tasks) =>
        Names.Select((name, i) => new Tag
        {
            Name = name,
            Owner = users[i % users.Count],
            WorkspaceId = tasks[0].WorkspaceId,
        }).ToList();
}
