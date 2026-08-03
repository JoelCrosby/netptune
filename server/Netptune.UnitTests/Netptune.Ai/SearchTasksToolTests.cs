using System.Text.Json;

using FluentAssertions;

using Mediator;

using Netptune.Ai.Tools;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.ProjectTasks;
using Netptune.Core.ViewModels.Tags;
using Netptune.Handlers.Tags.Queries;
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

    private Func<TaskFilter?> CaptureFilter()
    {
        TaskFilter? captured = null;

        Mediator
            .Send(Arg.Any<GetTasksQuery>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                captured = callInfo.Arg<GetTasksQuery>().Filter;

                return ClientResponse<PagedResponse<TaskViewModel>>.Success(CreatePage());
            });

        return () => captured;
    }

    private void StubTags(params string[] names)
    {
        var tags = names.Select(name => new TagViewModel { Name = name }).ToList();

        Mediator
            .Send(Arg.Any<GetTagsForWorkspaceQuery>(), Arg.Any<CancellationToken>())
            .Returns(tags);
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
