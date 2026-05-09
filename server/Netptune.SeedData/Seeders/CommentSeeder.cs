using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Entities.Contexts;

namespace Netptune.SeedData.Seeders;

public sealed class CommentSeeder : ISeeder
{
    public int Phase => 2;

    private static readonly string[] Bodies =
    [
        "This will cause an N+1 on the projects endpoint — should we `.Include()` the workspace here?",
        "Looks good overall. Let's add a test for the 401 case before merging.",
        "Confirmed fixed on staging. Closing this out.",
        "Do we need to handle the case where the token has already been revoked? Feels like an edge case we'll hit in production.",
        "The error message here is a bit cryptic for end users. Maybe 'Session expired, please sign in again'?",
        "I left some inline comments. Main concern is the lack of retry logic on the third-party call.",
        "We should gate this behind a feature flag until QA signs off.",
        "Reproduced locally. The issue is the background fetch interval resetting on foreground transition.",
        "Nice implementation. One suggestion: extract the validation logic into a separate service so we can unit test it independently.",
        "This is a breaking change for anyone using the v1 API. Should we version the endpoint?",
        "Performance looks good in the profiler. The p99 latency on that query dropped from 340ms to 42ms.",
        "Blocked on design approval for the new modal layout. Following up with the design team.",
        "Added the migration. Will hold off applying it to production until the next maintenance window.",
        "The Playwright test is flaky in CI — it times out waiting for the WebSocket connection. Will investigate.",
        "Done. Deployed to staging as part of the v1.4.2 release.",
        "Is there a reason we're not using the existing `RateLimitMiddleware` here? It already handles backoff.",
        "Left a comment on the PR. The logic looks correct but the variable names make it hard to follow.",
        "I can reproduce this with a fresh database but not in the shared dev environment. Possibly a migration ordering issue.",
        "The acceptance criteria in this ticket are a bit vague. Can we get the product team to clarify before we start?",
        "Approved. Ship it.",
    ];

    public async Task SeedAsync(DataContext dbContext, SeedContext context, CancellationToken ct)
    {
        context.Comments.AddRange(Enumerable.Range(0, Bodies.Length).Select(i =>
        {
            var task = context.Tasks[i % context.Tasks.Count];
            var workspaceUsers = context.UsersFor(task.Workspace);

            return new Comment
            {
                Body = Bodies[i],
                EntityId = task.Id,
                EntityType = EntityType.Task,
                Owner = workspaceUsers[i % workspaceUsers.Count],
                WorkspaceId = task.WorkspaceId,
            };
        }));

        await dbContext.Comments.AddRangeAsync(context.Comments, ct);
    }
}
