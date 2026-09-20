using System.Text.Json;

using FluentAssertions;

using Mediator;

using Netptune.Ai.Execution.Handlers;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Models.Ai;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services.Ai;
using Netptune.Core.ViewModels.Boards;
using Netptune.Handlers.BoardGroups.Commands;
using Netptune.Handlers.BoardGroups.Queries;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Ai;

public class BoardChangeHandlerTests
{
    private const int BoardId = 1;
    private const int BacklogId = 10;
    private const int DoingId = 11;
    private const int DoneId = 12;

    private readonly IMediator Mediator = Substitute.For<IMediator>();

    public BoardChangeHandlerTests()
    {
        GivenBoardGroups(BacklogId, DoingId, DoneId);
        GivenUpdateSucceeds();
    }

    [Fact]
    public async Task Reorder_ShouldSortTheGroupsIntoTheProposedOrder()
    {
        var payload = JsonSerializer.Serialize(new { boardId = BoardId, groupIds = new[] { DoneId, DoingId, BacklogId } });
        var handler = new ReorderBoardGroupsChangeHandler(Mediator);
        var context = CreateContext("propose_reorder_board_groups", "board", BoardId, payload);
        var result = await handler.Apply(context, TestContext.Current.CancellationToken);

        result.Status.Should().Be(AiChangeApplyStatus.Applied);

        var ordered = CapturedUpdates().Select(request => (request.BoardGroupId, request.SortOrder));

        ordered.Should().Equal((DoneId, 1D), (DoingId, 2D), (BacklogId, 3D));
    }

    [Fact]
    public async Task Reorder_ShouldLeaveEveryOtherFieldAlone()
    {
        var payload = JsonSerializer.Serialize(new { boardId = BoardId, groupIds = new[] { DoneId, DoingId, BacklogId } });
        var handler = new ReorderBoardGroupsChangeHandler(Mediator);
        var context = CreateContext("propose_reorder_board_groups", "board", BoardId, payload);

        await handler.Apply(context, TestContext.Current.CancellationToken);

        CapturedUpdates().Should().OnlyContain(request => request.Name == null && request.StatusId == null && !request.ClearStatus);
    }

    [Fact]
    public async Task Reorder_ShouldFail_WhenTheBoardNoLongerHasThoseGroups()
    {
        GivenBoardGroups(BacklogId, DoingId);

        var payload = JsonSerializer.Serialize(new { boardId = BoardId, groupIds = new[] { DoneId, DoingId, BacklogId } });
        var handler = new ReorderBoardGroupsChangeHandler(Mediator);
        var context = CreateContext("propose_reorder_board_groups", "board", BoardId, payload);
        var result = await handler.Apply(context, TestContext.Current.CancellationToken);

        result.Status.Should().Be(AiChangeApplyStatus.Failed);
        CapturedUpdates().Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateBoardGroup_ShouldFail_WhenTheGroupIsNotInTheWorkspace()
    {
        Mediator
            .Send(Arg.Any<GetBoardGroupQuery>(), Arg.Any<CancellationToken>())
            .Returns((BoardGroup?)null);

        var payload = JsonSerializer.Serialize(new { boardGroupId = DoingId, name = "In progress" });
        var handler = new UpdateBoardGroupChangeHandler(Mediator);
        var context = CreateContext("propose_update_board_group", "boardGroup", DoingId, payload);
        var result = await handler.Apply(context, TestContext.Current.CancellationToken);

        result.Status.Should().Be(AiChangeApplyStatus.Failed);
        CapturedUpdates().Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateBoardGroup_ShouldCarryTheClearedStatus()
    {
        Mediator
            .Send(Arg.Any<GetBoardGroupQuery>(), Arg.Any<CancellationToken>())
            .Returns(new BoardGroup { Id = DoingId, Name = "Doing", BoardId = BoardId });

        var payload = JsonSerializer.Serialize(new { boardGroupId = DoingId, clearStatus = true });
        var handler = new UpdateBoardGroupChangeHandler(Mediator);
        var context = CreateContext("propose_update_board_group", "boardGroup", DoingId, payload);
        var result = await handler.Apply(context, TestContext.Current.CancellationToken);

        result.Status.Should().Be(AiChangeApplyStatus.Applied);

        var request = CapturedUpdates().Should().ContainSingle().Subject;

        request.ClearStatus.Should().BeTrue();
        request.SortOrder.Should().BeNull();
    }

    private List<UpdateBoardGroupRequest> CapturedUpdates()
    {
        return Mediator.ReceivedCalls()
            .Select(call => call.GetArguments()[0])
            .OfType<UpdateBoardGroupCommand>()
            .Select(command => command.Request)
            .ToList();
    }

    private void GivenBoardGroups(params int[] ids)
    {
        var options = ids
            .Select(id => new BoardGroupOptionViewModel
            {
                Id = id,
                Name = $"Group {id}",
                BoardId = BoardId,
                BoardName = "Netptune",
                BoardIdentifier = "netptune",
                ProjectId = 3,
                ProjectName = "Netptune",
            })
            .ToList();

        Mediator
            .Send(Arg.Any<GetBoardGroupOptionsQuery>(), Arg.Any<CancellationToken>())
            .Returns(options);
    }

    private void GivenUpdateSucceeds()
    {
        var response = ClientResponse<BoardGroupViewModel>.Success(new BoardGroupViewModel { Name = "Group" });

        Mediator
            .Send(Arg.Any<UpdateBoardGroupCommand>(), Arg.Any<CancellationToken>())
            .Returns(response);
    }

    private static AiChangeApplyContext CreateContext(string toolName, string entityType, int entityId, string payload)
    {
        var change = new AiProposedChange
        {
            Id = 1,
            ChangeSetId = Guid.NewGuid(),
            Sequence = 1,
            ToolName = toolName,
            EntityType = entityType,
            EntityId = entityId,
            Summary = "Change a board",
            Payload = JsonDocument.Parse(payload),
            ValidationStatus = AiChangeValidationStatus.Valid,
            ApplyStatus = AiChangeApplyStatus.Pending,
        };

        return new AiChangeApplyContext
        {
            Change = change,
            ResolvedRefs = new Dictionary<string, int>(StringComparer.Ordinal),
        };
    }
}
