using System.Text.Json;

using FluentAssertions;

using Mediator;

using Netptune.Ai.Execution;
using Netptune.Ai.Tools;
using Netptune.Core.Authorization;
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
        var tool = new UpdateBoardAction(Mediator, ChangeSet);
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
        var tool = new UpdateBoardAction(Mediator, ChangeSet);
        var result = await Execute(tool, $$"""{"boardId":{{BoardId}},"identifier":"Team Delivery"}""");

        result.IsError.Should().BeFalse();
        ChangeSet.Changes[0].Payload.RootElement.GetProperty("identifier").GetString().Should().Be("team-delivery");
    }

    [Fact]
    public async Task UpdateBoard_ShouldFail_WhenTheIdentifierIsTaken()
    {
        GivenIdentifierIsTaken();

        var tool = new UpdateBoardAction(Mediator, ChangeSet);
        var result = await Execute(tool, $$"""{"boardId":{{BoardId}},"identifier":"delivery"}""");

        result.IsError.Should().BeTrue();
        ChangeSet.Changes.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateBoard_ShouldFail_WhenNothingWouldChange()
    {
        var tool = new UpdateBoardAction(Mediator, ChangeSet);
        var result = await Execute(tool, $$"""{"boardId":{{BoardId}},"name":"Netptune","identifier":"netptune"}""");

        result.IsError.Should().BeTrue();
        ChangeSet.Changes.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateBoard_ShouldFail_WhenTheBoardIsNotInTheWorkspace()
    {
        var tool = new UpdateBoardAction(Mediator, ChangeSet);
        var result = await Execute(tool, """{"boardId":404,"name":"Delivery"}""");

        result.IsError.Should().BeTrue();
        ChangeSet.Changes.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteBoard_ShouldProposeTheDeletion()
    {
        var tool = new DeleteBoardAction(Mediator, ChangeSet);
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

        var tool = new UpdateBoardGroupAction(Mediator, ChangeSet);
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

        var tool = new UpdateBoardGroupAction(Mediator, ChangeSet);
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

        var tool = new UpdateBoardGroupAction(Mediator, ChangeSet);
        var result = await Execute(tool, $$"""{"boardGroupId":{{DoingId}},"statusId":999}""");

        result.IsError.Should().BeTrue();
        ChangeSet.Changes.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateBoardGroup_ShouldFail_WhenNothingWouldChange()
    {
        GivenBoardGroup(DoingId, "Doing", InProgressStatusId);

        var tool = new UpdateBoardGroupAction(Mediator, ChangeSet);
        var result = await Execute(tool, $$"""{"boardGroupId":{{DoingId}},"name":"Doing","statusId":{{InProgressStatusId}}}""");

        result.IsError.Should().BeTrue();
        ChangeSet.Changes.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteBoardGroup_ShouldSayWhereItsTasksGo()
    {
        var tool = new DeleteBoardGroupAction(Mediator, ChangeSet);
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

        var tool = new DeleteBoardGroupAction(Mediator, ChangeSet);
        var result = await Execute(tool, $$"""{"boardGroupId":{{BacklogId}}}""");

        result.IsError.Should().BeTrue();
        ChangeSet.Changes.Should().BeEmpty();
    }

    [Fact]
    public async Task ReorderBoardGroups_ShouldProposeTheNewOrder()
    {
        var tool = new ReorderBoardGroupsAction(Mediator, ChangeSet);
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
        var tool = new ReorderBoardGroupsAction(Mediator, ChangeSet);
        var result = await Execute(tool, $$"""{"boardId":{{BoardId}},"groupIds":[{{DoneId}},{{BacklogId}}]}""");

        result.IsError.Should().BeTrue();
        result.Content.Should().Contain("exactly once");
        ChangeSet.Changes.Should().BeEmpty();
    }

    [Fact]
    public async Task ReorderBoardGroups_ShouldFail_WhenAGroupIsListedTwice()
    {
        var tool = new ReorderBoardGroupsAction(Mediator, ChangeSet);
        var result = await Execute(tool, $$"""{"boardId":{{BoardId}},"groupIds":[{{DoneId}},{{DoneId}},{{BacklogId}}]}""");

        result.IsError.Should().BeTrue();
        ChangeSet.Changes.Should().BeEmpty();
    }

    [Fact]
    public async Task ReorderBoardGroups_ShouldFail_WhenAGroupIsOnAnotherBoard()
    {
        var tool = new ReorderBoardGroupsAction(Mediator, ChangeSet);
        var result = await Execute(tool, $$"""{"boardId":{{BoardId}},"groupIds":[{{DoneId}},{{DoingId}},99]}""");

        result.IsError.Should().BeTrue();
        result.Content.Should().Contain("99");
        ChangeSet.Changes.Should().BeEmpty();
    }

    [Fact]
    public async Task ReorderBoardGroups_ShouldFail_WhenTheOrderIsTheOneItAlreadyHas()
    {
        var tool = new ReorderBoardGroupsAction(Mediator, ChangeSet);
        var result = await Execute(tool, $$"""{"boardId":{{BoardId}},"groupIds":[{{BacklogId}},{{DoingId}},{{DoneId}}]}""");

        result.IsError.Should().BeTrue();
        ChangeSet.Changes.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateBoardGroup_ShouldFail_WhenTheBoardAlreadyHasAGroupWithThatName()
    {
        var tool = new CreateBoardGroupAction(Mediator, ChangeSet);
        var result = await Execute(tool, $$"""{"boardId":{{BoardId}},"name":"backlog"}""");

        result.IsError.Should().BeTrue();
        ChangeSet.Changes.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateBoardGroup_ShouldFail_WhenTheSameGroupIsAlreadyProposedForTheBoard()
    {
        var tool = new CreateBoardGroupAction(Mediator, ChangeSet);

        await Execute(tool, $$"""{"boardId":{{BoardId}},"name":"Review"}""");
        var result = await Execute(tool, $$"""{"boardId":{{BoardId}},"name":"Review"}""");

        result.IsError.Should().BeTrue();
        ChangeSet.Changes.Should().ContainSingle();
    }

    [Fact]
    public async Task CreateBoardGroup_ShouldFail_WhenTheSameGroupIsAlreadyProposedForAPendingBoard()
    {
        GivenPendingBoard("board-1");

        var tool = new CreateBoardGroupAction(Mediator, ChangeSet);

        await Execute(tool, """{"boardRef":"board-1","name":"Todo"}""");
        var result = await Execute(tool, """{"boardRef":"board-1","name":"todo"}""");

        result.IsError.Should().BeTrue();
        ChangeSet.Changes.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateBoardGroup_ShouldPropose_WhenTheNameIsNewToTheBoard()
    {
        var tool = new CreateBoardGroupAction(Mediator, ChangeSet);
        var result = await Execute(tool, $$"""{"boardId":{{BoardId}},"name":"Review"}""");

        result.IsError.Should().BeFalse();
        ChangeSet.Changes.Should().ContainSingle(change => change.ToolName == "propose_create_board_group");
    }

    [Fact]
    public async Task CreateBoardGroup_ShouldAnswerWithAHandle_SoTasksCanBePlacedInIt()
    {
        GivenPendingBoard("board-1");

        var tool = new CreateBoardGroupAction(Mediator, ChangeSet);
        var result = await Execute(tool, """{"boardRef":"board-1","name":"Todo"}""");

        var change = ChangeSet.Changes.Last();

        change.EntityType.Should().Be("boardGroup");
        change.RefKey.Should().NotBeNull();
        result.Content.Should().Contain(change.RefKey);
    }

    [Fact]
    public async Task BoardGroupChange_ShouldProposeUnderTheActionsChangeName_WithoutTheActionInThePayload()
    {
        var tool = new BoardGroupChangeTool(Mediator, ChangeSet);
        var arguments = JsonDocument.Parse($$"""{"action":"create","boardId":{{BoardId}},"name":"Review"}""");

        var result = await tool.Execute(arguments.RootElement, TestContext.Current.CancellationToken);

        result.IsError.Should().BeFalse();

        var change = ChangeSet.Changes.Should().ContainSingle().Subject;

        change.ToolName.Should().Be("propose_create_board_group", "handlers and stored change sets key on the change name");
        change.Payload.RootElement.TryGetProperty("action", out _).Should().BeFalse(
            "the payload keeps the shape stored before the tools were merged");
    }

    [Fact]
    public async Task BoardChange_ShouldFail_WhenTheActionIsUnknown()
    {
        var tool = new BoardChangeTool(Mediator, ChangeSet);
        var arguments = JsonDocument.Parse($$"""{"action":"archive","boardId":{{BoardId}}}""");

        var result = await tool.Execute(arguments.RootElement, TestContext.Current.CancellationToken);

        result.IsError.Should().BeTrue();
        result.Content.Should().Contain("create, update, delete");
        ChangeSet.Changes.Should().BeEmpty();
    }

    [Theory]
    [InlineData("create", "propose_create_board_group", NetptunePermissions.BoardGroups.Create)]
    [InlineData("update", "propose_update_board_group", NetptunePermissions.BoardGroups.Update)]
    [InlineData("delete", "propose_delete_board_group", NetptunePermissions.BoardGroups.Delete)]
    [InlineData("reorder", "propose_reorder_board_groups", NetptunePermissions.BoardGroups.Update)]
    public void BoardGroupChange_ShouldAskOnlyForTheActionsPermission(string action, string changeName, string permission)
    {
        var tool = new BoardGroupChangeTool(Mediator, ChangeSet);
        var arguments = JsonDocument.Parse($$"""{"action":"{{action}}"}""").RootElement;

        tool.GetRequiredPermissions(arguments).Should().BeEquivalentTo([permission]);
        tool.GetChangePermissions(changeName, arguments).Should().BeEquivalentTo(
            [permission],
            "applying a change checks the permission of the action that proposed it");
    }

    [Fact]
    public void BoardChange_ShouldNameTheActionInTheWorkLog()
    {
        var tool = new BoardChangeTool(Mediator, ChangeSet);

        tool.DescribeCall(JsonDocument.Parse("""{"action":"delete"}""").RootElement)
            .Should().Be("propose_board_change · delete");
        tool.DescribeCall(JsonDocument.Parse("""{"action":"archive"}""").RootElement)
            .Should().Be("propose_board_change", "an action the tool does not know is not repeated back");
    }

    [Fact]
    public void BoardChange_ShouldBeOffered_WhenAnyActionIsPermitted()
    {
        var tool = new BoardChangeTool(Mediator, ChangeSet);

        tool.IsAvailable(new HashSet<string> { NetptunePermissions.Boards.Update }).Should().BeTrue();
        tool.IsAvailable(new HashSet<string> { NetptunePermissions.BoardGroups.Update }).Should().BeFalse();
    }

    private void GivenPendingBoard(string refKey)
    {
        ChangeSet.Add(new AiChangeDraft
        {
            ToolName = "propose_create_board",
            EntityType = "board",
            RefKey = refKey,
            Summary = "Create board Slash Commands",
            Fields = [new AiChangeField { Name = "name", After = "Slash Commands" }],
            Payload = JsonDocument.Parse("""{"name":"Slash Commands","identifier":"slash-commands","projectId":3}"""),
        });
    }

    private static async Task<AiToolExecution> Execute(IAiToolAction tool, string json)
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

        Mediator
            .Send(Arg.Is<GetBoardQuery>(query => query.Id == BoardId), Arg.Any<CancellationToken>())
            .Returns(ClientResponse<BoardViewModel>.Success(board));

        Mediator
            .Send(Arg.Is<GetBoardQuery>(query => query.Id != BoardId), Arg.Any<CancellationToken>())
            .Returns(ClientResponse<BoardViewModel>.NotFound);
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
