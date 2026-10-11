using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

using Netptune.Core.Models.Search;
using Netptune.Core.Services.Search;

using Xunit;

namespace Netptune.IntegrationTests.Search;

public sealed class TaskIdSearchTests
{
    private const int FaresTaskId = 900_001;
    private const int GatesTaskId = 900_002;
    private const int OtherWorkspaceTaskId = 900_003;
    private const int ApolloProjectId = 41;
    private const string AssigneeId = "assignee-\"quoted\"";

    private static readonly TimeSpan IndexingTimeout = TimeSpan.FromSeconds(20);

    private readonly NetptuneFixture Fixture;
    private readonly string WorkspaceSlug = $"search-{Guid.NewGuid():N}";

    public TaskIdSearchTests(NetptuneFixture fixture)
    {
        Fixture = fixture;
    }

    [Fact]
    public async Task SearchTaskIds_ShouldOnlyReturnTasksFromTheWorkspace()
    {
        var search = await IndexTasks();

        var result = await WaitForResults(search, new TaskIdSearchQuery("fares", WorkspaceSlug, 10), expected: 2);

        result.TaskIds.Should().BeEquivalentTo([FaresTaskId, GatesTaskId]);
        result.TaskIds.Should().NotContain(OtherWorkspaceTaskId);
    }

    [Fact]
    public async Task SearchTaskIds_ShouldFilterByProjectAndAssignee()
    {
        var search = await IndexTasks();

        await WaitForResults(search, new TaskIdSearchQuery("fares", WorkspaceSlug, 10), expected: 2);

        var byProject = await search.SearchTaskIdsAsync(
            new TaskIdSearchQuery("fares", WorkspaceSlug, 10, ProjectId: ApolloProjectId),
            TestContext.Current.CancellationToken);

        var byAssignee = await search.SearchTaskIdsAsync(
            new TaskIdSearchQuery("fares", WorkspaceSlug, 10, AssigneeId: AssigneeId),
            TestContext.Current.CancellationToken);

        byProject!.TaskIds.Should().Equal(FaresTaskId);
        byAssignee!.TaskIds.Should().Equal(FaresTaskId);
    }

    [Fact]
    public async Task SearchTaskIds_ShouldMatchDescriptions()
    {
        var search = await IndexTasks();

        var result = await WaitForResults(search, new TaskIdSearchQuery("turnstile", WorkspaceSlug, 10), expected: 1);

        result.TaskIds.Should().Equal(GatesTaskId);
    }

    private async Task<IMeilisearchService> IndexTasks()
    {
        var search = Fixture.Services.GetRequiredService<IMeilisearchService>();
        var cancellationToken = TestContext.Current.CancellationToken;

        await search.EnsureIndexSettingsAsync(cancellationToken);
        await search.IndexDocumentsAsync("tasks", new[]
        {
            Document(FaresTaskId, "Income from fares", null, WorkspaceSlug, ApolloProjectId, [AssigneeId]),
            Document(GatesTaskId, "Fare gates", "Stations need a turnstile at every entrance", WorkspaceSlug, ApolloProjectId + 1, []),
            Document(OtherWorkspaceTaskId, "Income from fares", null, $"other-{WorkspaceSlug}", ApolloProjectId, []),
        }, cancellationToken);

        return search;
    }

    private static async Task<TaskIdSearchResult> WaitForResults(IMeilisearchService search, TaskIdSearchQuery query, int expected)
    {
        var deadline = DateTime.UtcNow + IndexingTimeout;

        while (true)
        {
            var result = await search.SearchTaskIdsAsync(query, TestContext.Current.CancellationToken);
            var isIndexed = result is not null && result.TaskIds.Count >= expected;

            if (isIndexed || DateTime.UtcNow > deadline)
            {
                result.Should().NotBeNull("the search index should be reachable");

                return result!;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(200), TestContext.Current.CancellationToken);
        }
    }

    private static TaskSearchDocument Document(
        int taskId,
        string title,
        string? description,
        string workspaceSlug,
        int projectId,
        List<string> assigneeIds)
    {
        return new TaskSearchDocument
        {
            Id = $"task_{taskId}",
            TaskId = taskId,
            Title = title,
            Description = description,
            WorkspaceSlug = workspaceSlug,
            Status = "Todo",
            AssigneeIds = assigneeIds,
            ProjectId = projectId,
            ProjectScopeId = taskId,
            UpdatedAt = DateTime.UtcNow,
        };
    }
}
