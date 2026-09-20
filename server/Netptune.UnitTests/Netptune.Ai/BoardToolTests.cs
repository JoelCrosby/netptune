using System.Text.Json;

using FluentAssertions;

using Mediator;

using Netptune.Ai.Execution;
using Netptune.Ai.Tools;
using Netptune.Core.Entities;
using Netptune.Core.Responses;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services.Ai;
using Netptune.Core.ViewModels.Boards;
using Netptune.Core.ViewModels.Statuses;
using Netptune.Handlers.BoardGroups.Queries;
using Netptune.Handlers.Boards.Queries;
using Netptune.Handlers.Statuses.Queries;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Ai;

public class BoardToolTests
{
    private const int BoardId = 1;
    private const int BacklogId = 10;
    private const int DoingId = 11;
    private const int DoneId = 12;
    private const int InProgressStatusId = 5;

    private readonly IMediator Mediator = Substitute.For<IMediator>();
    private readonly AiChangeSetBuilder ChangeSet = new();

    public BoardToolTests()
    {
        GivenBoards();
        GivenBoardGroups();
        GivenStatuses();
        GivenIdentifierIsFree();
    }

    [Fact]
    public async Task UpdateBoard_ShouldProposeTheRename_AndLeaveTheIdentifierAlone()
    {
        var tool = new UpdateBoardTool(Mediator, ChangeSet);
        var result = await Execute(tool, $$"""{"boardId":{{BoardId}},"name":"Delivery"}""");

        result.IsError.Should().BeFalse();

        var change = ChangeSet.Changes.Should().ContainSingle().Subject;

        change.ToolName.Should().Be("propose_update_board");
        change.EntityType.Should().Be("board");
        change.EntityId.Should().Be(BoardId);
        change.Fields.Should().ContainSingle(field => field.Name == "name" && field.After == "Delivery");
        change.Payload.RootElement.TryGetProperty("identifier", out _).Should().BeFalse();
    }

    [Fact]
    public async Task UpdateBoard_ShouldSlugTheIdentifier()
    {
        var tool = new UpdateBoardTool(Mediator, ChangeSet);
        var result = await Execute(tool, $$"""{"boardId":{{BoardId}},"identifier":"Team Delivery"}""");

        result.IsError.Should().BeFalse();
        ChangeSet.Changes[0].Payload.RootElement.GetProperty("identifier").GetString().Should().Be("team-delivery");
    }

    [Fact]
    public async Task UpdateBoard_ShouldFail_WhenTheIdentifierIsTaken()
    {
        GivenIdentifierIsTaken();

        var tool = new UpdateBoardTool(Mediator, ChangeSet);
        var result = await Execute(tool, $$"""{"boardId":{{BoardId}},"identifier":"delivery"}""");

        result.IsError.Should().BeTrue();
        ChangeSet.Changes.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateBoard_ShouldFail_WhenNothingWouldChange()
    {
        var tool = new UpdateBoardTool(Mediator, ChangeSet);
        var result = await Execute(tool, $$"""{"boardId":{{BoardId}},"name":"Netptune","identifier":"netptune"}""");

        result.IsError.Should().BeTrue();
        ChangeSet.Changes.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateBoard_ShouldFail_WhenTheBoardIsNotInTheWorkspace()
    {
        var tool = new UpdateBoardTool(Mediator, ChangeSet);
        var result = await Execute(tool, """{"boardId":404,"name":"Delivery"}""");

        result.IsError.Should().BeTrue();
        ChangeSet.Changes.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteBoard_ShouldProposeTheDeletion()
    {
        var tool = new DeleteBoardTool(Mediator, ChangeSet);
        var result = await Execute(tool, $$"""{"boardId":{{BoardId}},"reason":"Replaced by the delivery board"}""");

        result.IsError.Should().BeFalse();

        var change = ChangeSet.Changes.Should().ContainSingle().Subject;

        change.ToolName.Should().Be("propose_delete_board");
        change.Summary.Should().Be("Delete board “Netptune” in Netptune");
        change.Fields.Should().Contain(field => field.Name == "reason");
        change.Fields.Should().Contain(field => field.Name == "tasks" && field.Before == "4");
        change.Payload.RootElement.GetProperty("boardId").GetInt32().Should().Be(BoardId);
    }

    [Fact]
    public async Task UpdateBoardGroup_ShouldProposeTheRenameAndTheStatus()
    {
        GivenBoardGroup(DoingId, "Doing", statusId: null);

        var tool = new UpdateBoardGroupTool(Mediator, ChangeSet);
        var result = await Execute(tool, $$"""{"boardGroupId":{{DoingId}},"name":"In progress","statusId":{{InProgressStatusId}}}""");

        result.IsError.Should().BeFalse();

        var change = ChangeSet.Changes.Should().ContainSingle().Subject;

        change.EntityType.Should().Be("boardGroup");
        change.EntityId.Should().Be(DoingId);
        change.Fields.Should().Contain(field => field.Name == "name" && field.After == "In progress");
        change.Fields.Should().Contain(field => field.Name == "status" && field.After == "In progress");
        change.Payload.RootElement.GetProperty("statusId").GetInt32().Should().Be(InProgressStatusId);
    }

    [Fact]
    public async Task UpdateBoardGroup_ShouldProposeClearingTheStatus()
    {
        GivenBoardGroup(DoingId, "Doing", InProgressStatusId);

        var tool = new UpdateBoardGroupTool(Mediator, ChangeSet);
        var result = await Execute(tool, $$"""{"boardGroupId":{{DoingId}},"clearStatus":true}""");

        result.IsError.Should().BeFalse();

        var change = ChangeSet.Changes.Should().ContainSingle().Subject;

        change.Fields.Should().ContainSingle(field => field.Name == "status" && field.Before == "In progress");
        change.Fields[0].After.Should().BeNull();
        change.Payload.RootElement.GetProperty("clearStatus").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task UpdateBoardGroup_ShouldFail_WhenTheStatusIsNotATaskStatus()
    {
        GivenBoardGroup(DoingId, "Doing", statusId: null);

        var tool = new UpdateBoardGroupTool(Mediator, ChangeSet);
        var result = await Execute(tool, $$"""{"boardGroupId":{{DoingId}},"statusId":999}""");

        result.IsError.Should().BeTrue();
        ChangeSet.Changes.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateBoardGroup_ShouldFail_WhenNothingWouldChange()
    {
        GivenBoardGroup(DoingId, "Doing", InProgressStatusId);

        var tool = new UpdateBoardGroupTool(Mediator, ChangeSet);
        var result = await Execute(tool, $$"""{"boardGroupId":{{DoingId}},"name":"Doing","statusId":{{InProgressStatusId}}}""");

        result.IsError.Should().BeTrue();
        ChangeSet.Changes.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteBoardGroup_ShouldSayWhereItsTasksGo()
    {
        var tool = new DeleteBoardGroupTool(Mediator, ChangeSet);
        var result = await Execute(tool, $$"""{"boardGroupId":{{DoingId}}}""");

        result.IsError.Should().BeFalse();
        result.Content.Should().Contain("Backlog");

        var change = ChangeSet.Changes.Should().ContainSingle().Subject;

        change.ToolName.Should().Be("propose_delete_board_group");
        change.EntityId.Should().Be(DoingId);
        change.Fields.Should().Contain(field => field.Name == "tasksMoveTo" && field.After == "Backlog");
    }

    [Fact]
    public async Task DeleteBoardGroup_ShouldRefuse_WhenItIsTheLastGroupOnTheBoard()
    {
        GivenBoardGroups(Group(BacklogId, "Backlog"));

        var tool = new DeleteBoardGroupTool(Mediator, ChangeSet);
        var result = await Execute(tool, $$"""{"boardGroupId":{{BacklogId}}}""");

        result.IsError.Should().BeTrue();
        ChangeSet.Changes.Should().BeEmpty();
    }

    [Fact]
    public async Task ReorderBoardGroups_ShouldProposeTheNewOrder()
    {
        var tool = new ReorderBoardGroupsTool(Mediator, ChangeSet);
        var result = await Execute(tool, $$"""{"boardId":{{BoardId}},"groupIds":[{{DoneId}},{{DoingId}},{{BacklogId}}]}""");

        result.IsError.Should().BeFalse();

        var change = ChangeSet.Changes.Should().ContainSingle().Subject;

        change.ToolName.Should().Be("propose_reorder_board_groups");
        change.EntityType.Should().Be("board");
        change.Fields.Should().ContainSingle(field => field.Name == "order");
        change.Fields[0].Before.Should().Be("Backlog → Doing → Done");
        change.Fields[0].After.Should().Be("Done → Doing → Backlog");
        change.Payload.RootElement.GetProperty("groupIds").GetArrayLength().Should().Be(3);
    }

    [Fact]
    public async Task ReorderBoardGroups_ShouldFail_WhenAGroupIsLeftOut()
    {
        var tool = new ReorderBoardGroupsTool(Mediator, ChangeSet);
        var result = await Execute(tool, $$"""{"boardId":{{BoardId}},"groupIds":[{{DoneId}},{{BacklogId}}]}""");

        result.IsError.Should().BeTrue();
        result.Content.Should().Contain("exactly once");
        ChangeSet.Changes.Should().BeEmpty();
    }

    [Fact]
    public async Task ReorderBoardGroups_ShouldFail_WhenAGroupIsListedTwice()
    {
        var tool = new ReorderBoardGroupsTool(Mediator, ChangeSet);
        var result = await Execute(tool, $$"""{"boardId":{{BoardId}},"groupIds":[{{DoneId}},{{DoneId}},{{BacklogId}}]}""");

        result.IsError.Should().BeTrue();
        ChangeSet.Changes.Should().BeEmpty();
    }

    [Fact]
    public async Task ReorderBoardGroups_ShouldFail_WhenAGroupIsOnAnotherBoard()
    {
        var tool = new ReorderBoardGroupsTool(Mediator, ChangeSet);
        var result = await Execute(tool, $$"""{"boardId":{{BoardId}},"groupIds":[{{DoneId}},{{DoingId}},99]}""");

        result.IsError.Should().BeTrue();
        result.Content.Should().Contain("99");
        ChangeSet.Changes.Should().BeEmpty();
    }

    [Fact]
    public async Task ReorderBoardGroups_ShouldFail_WhenTheOrderIsTheOneItAlreadyHas()
    {
        var tool = new ReorderBoardGroupsTool(Mediator, ChangeSet);
        var result = await Execute(tool, $$"""{"boardId":{{BoardId}},"groupIds":[{{BacklogId}},{{DoingId}},{{DoneId}}]}""");

        result.IsError.Should().BeTrue();
        ChangeSet.Changes.Should().BeEmpty();
    }

    private static async Task<AiToolExecution> Execute(IAiTool tool, string json)
    {
        var arguments = JsonDocument.Parse(json).RootElement;

        return await tool.Execute(arguments, TestContext.Current.CancellationToken);
    }

    private void GivenBoards()
    {
        var board = new BoardViewModel
        {
            Id = BoardId,
            Name = "Netptune",
            Identifier = "netptune",
            ProjectId = 3,
            ProjectName = "Netptune",
            TaskCount = 4,
        };

        var project = new BoardsViewModel
        {
            ProjectId = 3,
            ProjectName = "Netptune",
            Boards = [board],
        };

        Mediator
            .Send(Arg.Any<GetBoardsInWorkspaceQuery>(), Arg.Any<CancellationToken>())
            .Returns(new List<BoardsViewModel> { project });
    }

    private void GivenBoardGroups()
    {
        GivenBoardGroups(Group(BacklogId, "Backlog"), Group(DoingId, "Doing"), Group(DoneId, "Done"));
    }

    private void GivenBoardGroups(params BoardGroupOptionViewModel[] groups)
    {
        Mediator
            .Send(Arg.Any<GetBoardGroupOptionsQuery>(), Arg.Any<CancellationToken>())
            .Returns(groups.ToList());
    }

    private void GivenBoardGroup(int id, string name, int? statusId)
    {
        Mediator
            .Send(Arg.Any<GetBoardGroupQuery>(), Arg.Any<CancellationToken>())
            .Returns(new BoardGroup
            {
                Id = id,
                Name = name,
                BoardId = BoardId,
                StatusId = statusId,
            });
    }

    private void GivenStatuses()
    {
        var status = new StatusViewModel
        {
            Id = InProgressStatusId,
            Name = "In progress",
            Key = "in-progress",
            Color = "blue",
        };

        Mediator
            .Send(Arg.Any<GetStatusesQuery>(), Arg.Any<CancellationToken>())
            .Returns(new List<StatusViewModel> { status });
    }

    private void GivenIdentifierIsFree()
    {
        GivenIdentifier(true);
    }

    private void GivenIdentifierIsTaken()
    {
        GivenIdentifier(false);
    }

    private void GivenIdentifier(bool isUnique)
    {
        var response = ClientResponse<IsSlugUniqueResponse>.Success(new IsSlugUniqueResponse { IsUnique = isUnique });

        Mediator
            .Send(Arg.Any<IsBoardIdentifierUniqueQuery>(), Arg.Any<CancellationToken>())
            .Returns(response);
    }

    private static BoardGroupOptionViewModel Group(int id, string name)
    {
        return new BoardGroupOptionViewModel
        {
            Id = id,
            Name = name,
            BoardId = BoardId,
            BoardName = "Netptune",
            BoardIdentifier = "netptune",
            ProjectId = 3,
            ProjectName = "Netptune",
        };
    }
}
