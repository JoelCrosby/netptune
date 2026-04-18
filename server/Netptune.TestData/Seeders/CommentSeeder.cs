using Netptune.Core.Entities;
using Netptune.Core.Enums;

namespace Netptune.TestData.Seeders;

internal static class CommentSeeder
{
    internal static List<Comment> Generate(List<ProjectTask> tasks, List<AppUser> users) =>
        Enumerable.Range(0, 32).Select(i => new Comment
        {
            Body = $"Comment {i + 1}",
            EntityId = tasks[i % tasks.Count].Id,
            EntityType = EntityType.Task,
            Owner = users[i % users.Count],
            WorkspaceId = tasks[i % tasks.Count].WorkspaceId,
        }).ToList();
}
