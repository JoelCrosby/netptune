using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using FluentAssertions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.Activity;
using Netptune.Entities.Contexts;

using Xunit;

namespace Netptune.IntegrationTests.Endpoints;

public sealed class ActivityBoardFeedTests
{
    private readonly NetptuneFixture Fixture;
    private readonly HttpClient Client;

    public ActivityBoardFeedTests(NetptuneFixture fixture)
    {
        Fixture = fixture;
        Client = fixture.CreateNetptuneClient();
    }

    [Fact]
    public async Task BoardFeed_ShouldIncludeActivityOnTheBoardsTasksButNotOtherBoards()
    {
        var (workspaceId, boardId, otherBoardId, task) = await SeedContext();
        var marker = Guid.NewGuid().ToString("N");

        await AddEntry(workspaceId, EntityType.Board, boardId, boardId, marker, "board");
        await AddEntry(workspaceId, EntityType.Task, task.Id, boardId, marker, "task-on-board");
        await AddEntry(workspaceId, EntityType.Task, task.Id, otherBoardId, marker, "task-on-other-board");

        var feed = await GetBoardFeed(boardId);
        var labels = feed
            .Where(activity => ReadMeta(activity, "marker") == marker)
            .Select(activity => ReadMeta(activity, "label"))
            .ToList();

        labels.Should().BeEquivalentTo(["board", "task-on-board"]);
    }

    [Fact]
    public async Task BoardFeed_ShouldNameTheTaskAndAllowEntriesWithNoUser()
    {
        var (workspaceId, boardId, _, task) = await SeedContext();
        var marker = Guid.NewGuid().ToString("N");

        await AddEntry(workspaceId, EntityType.Task, task.Id, boardId, marker, "no-user", hasUser: false);

        var feed = await GetBoardFeed(boardId);
        var entry = feed.Single(activity => ReadMeta(activity, "marker") == marker);

        entry.UserId.Should().BeNull();
        entry.EntityType.Should().Be(EntityType.Task);
        entry.TaskName.Should().Be(task.Name);
        entry.TaskSystemId.Should().Be(task.SystemId);
    }

    private async Task<List<ActivityViewModel>> GetBoardFeed(int boardId)
    {
        var response = await Client.GetAsync(
            $"api/activity/{EntityType.Board}/{boardId}?take=100",
            TestContext.Current.CancellationToken);
        var result = await response.Content.ReadFromJsonAsync<ClientResponse<List<ActivityViewModel>>>(
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        return result.Payload!;
    }

    private async Task<(int WorkspaceId, int BoardId, int OtherBoardId, (int Id, string Name, string SystemId) Task)>
        SeedContext()
    {
        using var scope = Fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();
        var token = TestContext.Current.CancellationToken;

        var boards = await context.Boards
            .AsNoTracking()
            .Where(board => !board.IsDeleted && board.Workspace!.Slug == "netptune")
            .OrderBy(board => board.Id)
            .Select(board => new { board.Id, board.WorkspaceId })
            .Take(2)
            .ToListAsync(token);

        var task = await context.ProjectTasks
            .AsNoTracking()
            .Where(candidate => !candidate.IsDeleted && candidate.Workspace!.Slug == "netptune")
            .OrderBy(candidate => candidate.Id)
            .Select(candidate => new { candidate.Id, candidate.Name, candidate.Project!.Key, candidate.ProjectScopeId })
            .FirstAsync(token);

        return (boards[0].WorkspaceId, boards[0].Id, boards[1].Id, (task.Id, task.Name, $"{task.Key}-{task.ProjectScopeId}"));
    }

    private async Task AddEntry(
        int workspaceId,
        EntityType entityType,
        int entityId,
        int boardId,
        string marker,
        string label,
        bool hasUser = true)
    {
        using var scope = Fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();
        var token = TestContext.Current.CancellationToken;
        var now = DateTime.UtcNow;

        var anyUserId = await context.Users.AsNoTracking().Select(user => user.Id).FirstAsync(token);

        context.ActivityEntries.Add(new ActivityEntry
        {
            WorkspaceId = workspaceId,
            EntityType = entityType,
            EntityId = entityId,
            BoardId = boardId,
            UserId = hasUser ? anyUserId : null,
            ActivityType = ActivityType.PullRequestMerged,
            Meta = JsonSerializer.SerializeToDocument(new { marker, label }),
            FirstOccurredAt = now,
            LastOccurredAt = now,
            WindowExpiresAt = now,
            NotifiedAt = now,
            RevisionCount = 1,
        });

        await context.SaveChangesAsync(token);
    }

    private static string? ReadMeta(ActivityViewModel activity, string property)
    {
        var meta = activity.Meta?.RootElement;
        var hasProperty = meta is { ValueKind: JsonValueKind.Object } && meta.Value.TryGetProperty(property, out _);

        return hasProperty ? meta!.Value.GetProperty(property).GetString() : null;
    }
}
