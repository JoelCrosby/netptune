using System.Text.Json;

using FluentAssertions;

using Mediator;

using Netptune.Ai.Tools;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services.Search;
using Netptune.Core.ViewModels.ProjectTasks;
using Netptune.Core.ViewModels.Tags;
using Netptune.Handlers.Search.Queries;
using Netptune.Handlers.Tasks.Queries;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Ai;

public class SearchTasksToolTests
{
    private readonly IMediator Mediator = Substitute.For<IMediator>();

    [Fact]
    public async Task ShouldPassTheTagPresenceFilterThrough()
    {
        var captured = CaptureFilter();
        var tool = new SearchTasksTool(Mediator);

        var result = await tool.Execute(Arguments("""{"hasTags":false}"""), TestContext.Current.CancellationToken);

        result.IsError.Should().BeFalse();
        captured().Should().NotBeNull();
        captured()!.HasTags.Should().BeFalse();
    }

    [Fact]
    public async Task ShouldLeaveTheTagPresenceFilterUnset_WhenItWasNotRequested()
    {
        var captured = CaptureFilter();
        var tool = new SearchTasksTool(Mediator);

        var result = await tool.Execute(Arguments("{}"), TestContext.Current.CancellationToken);

        result.IsError.Should().BeFalse();
        captured()!.HasTags.Should().BeNull();
        captured()!.Tags.Should().BeEmpty();
    }

    [Fact]
    public async Task ShouldPassTheAssigneePresenceFilterThrough()
    {
        var captured = CaptureFilter();
        var tool = new SearchTasksTool(Mediator);

        var result = await tool.Execute(Arguments("""{"hasAssignee":false}"""), TestContext.Current.CancellationToken);

        result.IsError.Should().BeFalse();
        captured()!.HasAssignee.Should().BeFalse();
        captured()!.Assignees.Should().BeEmpty();
    }

    [Fact]
    public async Task ShouldLeaveTheAssigneePresenceFilterUnset_WhenItWasNotRequested()
    {
        var captured = CaptureFilter();
        var tool = new SearchTasksTool(Mediator);

        var result = await tool.Execute(Arguments("""{"assigneeId":"user-1"}"""), TestContext.Current.CancellationToken);

        result.IsError.Should().BeFalse();
        captured()!.HasAssignee.Should().BeNull();
        captured()!.Assignees.Should().Equal("user-1");
    }

    [Fact]
    public async Task ShouldMatchRequestedTagsToWorkspaceCasing()
    {
        var captured = CaptureFilter();

        StubTags("Typescript", "Backend");

        var tool = new SearchTasksTool(Mediator);
        var result = await tool.Execute(Arguments("""{"tags":["typescript"]}"""), TestContext.Current.CancellationToken);

        result.IsError.Should().BeFalse();
        captured()!.Tags.Should().Equal("Typescript");
    }

    [Fact]
    public async Task ShouldFail_WhenARequestedTagDoesNotExist()
    {
        CaptureFilter();
        StubTags("Typescript");

        var tool = new SearchTasksTool(Mediator);
        var result = await tool.Execute(Arguments("""{"tags":["Nonsense"]}"""), TestContext.Current.CancellationToken);

        result.IsError.Should().BeTrue();
        result.Content.Should().Contain("Nonsense");
    }

    [Fact]
    public async Task ShouldReportTheTagsOnEachTask()
    {
        CaptureFilter();

        var tool = new SearchTasksTool(Mediator);
        var result = await tool.Execute(Arguments("{}"), TestContext.Current.CancellationToken);

        result.IsError.Should().BeFalse();

        using var content = JsonDocument.Parse(result.Content);

        var tags = content.RootElement.GetProperty("tasks")[0].GetProperty("tags");

        tags.EnumerateArray().Select(tag => tag.GetString()).Should().Equal("Typescript");
    }

    [Fact]
    public async Task ShouldRankFreeTextMatchesByTheSearchIndex()
    {
        StubIndex(new TaskIdSearchResult([3, 1], 2));

        var captured = CaptureFilter(NewTask(1, "Fares"), NewTask(3, "Income from fares"));
        var tool = new SearchTasksTool(Mediator);
        var result = await tool.Execute(Arguments("""{"search":"fare income","statusId":4}"""), TestContext.Current.CancellationToken);

        result.IsError.Should().BeFalse();
        captured()!.TaskIds.Should().Equal(3, 1);
        captured()!.Search.Should().BeNull();
        captured()!.StatusIds.Should().Equal(4);

        using var content = JsonDocument.Parse(result.Content);

        content.RootElement.GetProperty("searchMode").GetString().Should().Be("relevance");
        TaskIds(content).Should().Equal(3, 1);
    }

    [Fact]
    public async Task ShouldFallBackToNameSearch_WhenTheSearchIndexIsUnavailable()
    {
        StubIndex(null);

        var captured = CaptureFilter();
        var tool = new SearchTasksTool(Mediator);
        var result = await tool.Execute(Arguments("""{"search":"stale"}"""), TestContext.Current.CancellationToken);

        result.IsError.Should().BeFalse();
        captured()!.Search.Should().Be("stale");
        captured()!.TaskIds.Should().BeEmpty();
    }

    [Fact]
    public async Task ShouldFallBackToNameSearch_WhenTheSearchIndexFindsNothing()
    {
        StubIndex(new TaskIdSearchResult([], 0));

        var captured = CaptureFilter();
        var tool = new SearchTasksTool(Mediator);

        await tool.Execute(Arguments("""{"search":"just created"}"""), TestContext.Current.CancellationToken);

        captured()!.Search.Should().Be("just created");
    }

    [Fact]
    public async Task ShouldLookUpASystemIdWithoutTheSearchIndex()
    {
        var captured = CaptureFilter();
        var tool = new SearchTasksTool(Mediator);

        await tool.Execute(Arguments("""{"search":"rail-5"}"""), TestContext.Current.CancellationToken);

        captured()!.Search.Should().Be("rail-5");
        await Mediator.DidNotReceive().Send(Arg.Any<SearchTaskIdsQuery>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldPassTheBoardColumnFiltersThrough()
    {
        var captured = CaptureFilter();
        var tool = new SearchTasksTool(Mediator);

        await tool.Execute(Arguments("""{"statusId":24,"notInBoardGroupId":37}"""), TestContext.Current.CancellationToken);

        captured()!.NotInBoardGroupId.Should().Be(37);
        captured()!.BoardGroupId.Should().BeNull();
    }

    [Fact]
    public async Task ShouldReportEachTasksColumnsAndAnExcerpt()
    {
        var task = NewTask(5, "Demolish tool") with
        {
            Description = new string('x', 400),
            SprintName = "Feature Gaps Plan",
            Placements =
            [
                new BoardPlacementViewModel { BoardName = "Rails", BoardGroupName = "In Progress", BoardGroupId = 35, BoardIdentifier = "rail" },
            ],
        };

        CaptureFilter(task);

        var tool = new SearchTasksTool(Mediator);
        var result = await tool.Execute(Arguments("{}"), TestContext.Current.CancellationToken);

        using var content = JsonDocument.Parse(result.Content);

        var row = content.RootElement.GetProperty("tasks")[0];
        var column = row.GetProperty("columns")[0];

        column.GetProperty("column").GetString().Should().Be("In Progress");
        column.GetProperty("boardGroupId").GetInt32().Should().Be(35);
        row.GetProperty("sprint").GetString().Should().Be("Feature Gaps Plan");
        row.GetProperty("excerpt").GetString()!.Length.Should().BeLessThan(200);
        row.TryGetProperty("dueDate", out _).Should().BeFalse("null fields are left out to save space");
    }

    [Fact]
    public async Task ShouldStayWithinTheResultBudget_WhenTasksAreLarge()
    {
        var tasks = Enumerable.Range(1, 100)
            .Select(id => NewTask(id, new string('n', 400)) with { Description = new string('d', 2000) })
            .ToArray();

        CaptureFilter(tasks);

        var tool = new SearchTasksTool(Mediator);
        var result = await tool.Execute(Arguments("""{"pageSize":100}"""), TestContext.Current.CancellationToken);

        result.Content.Length.Should().BeLessThan(32_000);

        using var content = JsonDocument.Parse(result.Content);

        content.RootElement.GetProperty("omittedForSize").GetInt32().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ShouldPageThroughRankedMatches()
    {
        StubIndex(new TaskIdSearchResult([1, 2, 3], 3));
        CaptureFilter(NewTask(1, "One"), NewTask(2, "Two"), NewTask(3, "Three"));

        var tool = new SearchTasksTool(Mediator);
        var result = await tool.Execute(Arguments("""{"search":"number","page":2,"pageSize":2}"""), TestContext.Current.CancellationToken);

        using var content = JsonDocument.Parse(result.Content);

        content.RootElement.GetProperty("totalCount").GetInt32().Should().Be(3);
        TaskIds(content).Should().Equal(3);
    }

    private Func<TaskFilter?> CaptureFilter(params TaskViewModel[] tasks)
    {
        TaskFilter? captured = null;
        var page = tasks.Length == 0 ? CreatePage() : new PagedResponse<TaskViewModel>([.. tasks], 1, tasks.Length, tasks.Length);

        Mediator
            .Send(Arg.Any<GetTasksQuery>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                captured = callInfo.Arg<GetTasksQuery>().Filter;

                return ClientResponse<PagedResponse<TaskViewModel>>.Success(page);
            });

        return () => captured;
    }

    private void StubIndex(TaskIdSearchResult? result)
    {
        Mediator
            .Send(Arg.Any<SearchTaskIdsQuery>(), Arg.Any<CancellationToken>())
            .Returns(result);
    }

    private static TaskViewModel NewTask(int id, string name)
    {
        return new TaskViewModel { Id = id, Name = name, SystemId = $"rail-{id}", StatusName = "Done" };
    }

    private static List<int> TaskIds(JsonDocument content)
    {
        return content.RootElement.GetProperty("tasks").EnumerateArray()
            .Select(task => task.GetProperty("taskId").GetInt32())
            .ToList();
    }

    private void StubTags(params string[] names)
    {
        var tags = names.Select(name => new TagViewModel { Name = name }).ToList();

        AiTestTags.Given(Mediator, tags);
    }

    private static PagedResponse<TaskViewModel> CreatePage()
    {
        var task = new TaskViewModel
        {
            Id = 1,
            Name = "Ship the assistant",
            Tags = ["Typescript"],
        };

        return new PagedResponse<TaskViewModel>([task], 1, 25, 1);
    }

    private static JsonElement Arguments(string json)
    {
        return JsonDocument.Parse(json).RootElement;
    }
}
