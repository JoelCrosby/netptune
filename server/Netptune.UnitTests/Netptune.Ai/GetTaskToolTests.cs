using System.Text.Json;

using FluentAssertions;

using Mediator;

using Netptune.Ai.Tools;
using Netptune.Core.Authorization;
using Netptune.Core.Services;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services.Ai;
using Netptune.Core.SourceControl;
using Netptune.Core.SourceControl.Models;
using Netptune.Core.ViewModels.Comments;
using Netptune.Core.ViewModels.ProjectTasks;
using Netptune.Core.ViewModels.SourceControl;
using Netptune.Handlers.Comments.Queries;
using Netptune.Handlers.SourceControl.Queries;
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
        content.RootElement.TryGetProperty("code", out _).Should().BeFalse();
    }

    [Fact]
    public async Task Execute_ShouldReadTheLinkedCode_LeavingOutRemovedLinks_WhenIncluded()
    {
        GivenTask();

        var pullRequest = new TaskCodeLinkViewModel
        {
            Kind = TaskCodeLinkKind.PullRequest,
            ExternalRef = "18",
            Title = "Fix the login redirect",
            WebUrl = "https://github.com/acme/web/pull/18",
            PullRequestState = ScmPullRequestState.Open,
            RepoFullName = "acme/web",
            RepoWebUrl = "https://github.com/acme/web",
        };

        var removedBranch = pullRequest with
        {
            Kind = TaskCodeLinkKind.Branch,
            ExternalRef = "old-branch",
            PullRequestState = null,
            IsRemoved = true,
        };

        Mediator
            .Send(Arg.Any<GetTaskCodeLinksQuery>(), Arg.Any<CancellationToken>())
            .Returns(ClientResponse<List<TaskCodeLinkViewModel>>.Success([pullRequest, removedBranch]));

        var result = await Execute($$"""{"systemId":"{{SystemId}}","include":["code"]}""");

        result.IsError.Should().BeFalse();

        using var content = JsonDocument.Parse(result.Content);
        var link = content.RootElement.GetProperty("code").EnumerateArray().Should().ContainSingle().Subject;

        link.GetProperty("kind").GetString().Should().Be("PullRequest");
        link.GetProperty("repository").GetString().Should().Be("acme/web");
        link.GetProperty("ref").GetString().Should().Be("18");
        link.GetProperty("pullRequestState").GetString().Should().Be("Open");
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
        var withCode = JsonDocument.Parse($$"""{"systemId":"{{SystemId}}","include":["code"]}""").RootElement;

        tool.GetRequiredPermissions(plain).Should().BeEquivalentTo([NetptunePermissions.Tasks.Read]);
        tool.GetRequiredPermissions(withExtras)
            .Should()
            .BeEquivalentTo([NetptunePermissions.Tasks.Read, NetptunePermissions.Comments.Read, NetptunePermissions.Files.Read]);
        tool.GetRequiredPermissions(withCode)
            .Should()
            .BeEquivalentTo([NetptunePermissions.Tasks.Read, NetptunePermissions.SourceControl.Read]);
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
