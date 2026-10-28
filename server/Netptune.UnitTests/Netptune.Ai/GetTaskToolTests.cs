using System.Text.Json;

using FluentAssertions;

using Mediator;

using Netptune.Ai.Tools;
using Netptune.Core.Authorization;
using Netptune.Core.Services;
using Netptune.Core.Services.Ai;
using Netptune.Core.ViewModels.Comments;
using Netptune.Core.ViewModels.ProjectTasks;
using Netptune.Handlers.Comments.Queries;
using Netptune.Handlers.Tasks.Queries;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Ai;

public class GetTaskToolTests
{
    private const string SystemId = "NPT-42";

    private readonly IMediator Mediator = Substitute.For<IMediator>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();

    [Fact]
    public async Task Execute_ShouldLeaveOutWhatWasNotIncluded()
    {
        GivenTask();

        var result = await Execute($$"""{"systemId":"{{SystemId}}"}""");

        result.IsError.Should().BeFalse();

        using var content = JsonDocument.Parse(result.Content);

        content.RootElement.TryGetProperty("comments", out _).Should().BeFalse();
        content.RootElement.TryGetProperty("relations", out _).Should().BeFalse();
        content.RootElement.TryGetProperty("files", out _).Should().BeFalse();
    }

    [Fact]
    public async Task Execute_ShouldListEveryBoardTheTaskIsOn()
    {
        GivenTask();

        var result = await Execute($$"""{"systemId":"{{SystemId}}"}""");

        result.IsError.Should().BeFalse();

        using var content = JsonDocument.Parse(result.Content);
        var placements = content.RootElement.GetProperty("placements").EnumerateArray().ToList();

        placements.Select(placement => placement.GetProperty("boardId").GetInt32()).Should().Equal(4, 5);
        placements.Select(placement => placement.GetProperty("boardGroupId").GetInt32()).Should().Equal(9, 11);
    }

    [Fact]
    public async Task Execute_ShouldReadCommentsAlongsideTheTask_WhenIncluded()
    {
        GivenTask();

        Mediator
            .Send(Arg.Any<GetCommentsForTaskQuery>(), Arg.Any<CancellationToken>())
            .Returns([new CommentViewModel { Id = 7, Body = "Looks good", UserDisplayName = "Joel" }]);

        var result = await Execute($$"""{"systemId":"{{SystemId}}","include":["comments"]}""");

        result.IsError.Should().BeFalse();

        using var content = JsonDocument.Parse(result.Content);
        var comment = content.RootElement.GetProperty("comments").EnumerateArray().Single();

        content.RootElement.GetProperty("name").GetString().Should().Be("Fix the login page");
        comment.GetProperty("body").GetString().Should().Be("Looks good");
    }

    [Fact]
    public async Task Execute_ShouldFitALongThreadIntoTheBudget_AndSayHowManyWereLeftOut()
    {
        GivenTask();

        var comments = Enumerable.Range(1, 50)
            .Select(id => new CommentViewModel { Id = id, Body = new string('x', 10_000), UserDisplayName = "Joel" })
            .ToList();

        Mediator
            .Send(Arg.Any<GetCommentsForTaskQuery>(), Arg.Any<CancellationToken>())
            .Returns(comments);

        var result = await Execute($$"""{"systemId":"{{SystemId}}","include":["comments"]}""");

        using var content = JsonDocument.Parse(result.Content);
        var root = content.RootElement;

        result.Content.Length.Should().BeLessThan(32_000);
        root.GetProperty("commentsOmitted").GetInt32().Should().BePositive();
        root.GetProperty("comments").EnumerateArray().First().GetProperty("body").GetString()
            .Should().EndWith("(comment shortened)");
    }

    [Fact]
    public async Task Execute_ShouldFail_WhenAnIncludeIsUnknown()
    {
        var result = await Execute($$"""{"systemId":"{{SystemId}}","include":["history"]}""");

        result.IsError.Should().BeTrue();
    }

    [Fact]
    public void GetRequiredPermissions_ShouldDemandTheReadPermissionOfEachInclude()
    {
        var tool = new GetTaskTool(Mediator, Identity);
        var plain = JsonDocument.Parse($$"""{"systemId":"{{SystemId}}"}""").RootElement;
        var withExtras = JsonDocument.Parse($$"""{"systemId":"{{SystemId}}","include":["comments","files"]}""").RootElement;

        tool.GetRequiredPermissions(plain).Should().BeEquivalentTo([NetptunePermissions.Tasks.Read]);
        tool.GetRequiredPermissions(withExtras)
            .Should()
            .BeEquivalentTo([NetptunePermissions.Tasks.Read, NetptunePermissions.Comments.Read, NetptunePermissions.Files.Read]);
    }

    private async Task<AiToolExecution> Execute(string arguments)
    {
        var tool = new GetTaskTool(Mediator, Identity);
        var element = JsonDocument.Parse(arguments).RootElement;

        return await tool.Execute(element, TestContext.Current.CancellationToken);
    }

    private void GivenTask()
    {
        var task = new TaskViewModel
        {
            Id = 42,
            SystemId = SystemId,
            Name = "Fix the login page",
            Placements =
            [
                new BoardPlacementViewModel
                {
                    BoardId = 4,
                    BoardName = "Netptune",
                    BoardIdentifier = "netptune",
                    BoardGroupId = 9,
                    BoardGroupName = "Shipped",
                },
                new BoardPlacementViewModel
                {
                    BoardId = 5,
                    BoardName = "Slash Commands",
                    BoardIdentifier = "slash-commands",
                    BoardGroupId = 11,
                    BoardGroupName = "Todo",
                },
            ],
        };

        Mediator
            .Send(Arg.Any<GetTaskDetailQuery>(), Arg.Any<CancellationToken>())
            .Returns(task);
    }
}
