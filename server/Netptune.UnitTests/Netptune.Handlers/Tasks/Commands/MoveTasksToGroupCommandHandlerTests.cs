using AutoFixture;

using FluentAssertions;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Events;
using Netptune.Core.Events.Tasks;
using Netptune.Core.Models.ProjectTasks;
using Netptune.Core.Models.Search;
using Netptune.Core.Requests;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.Services.ProjectTasks;
using Netptune.Core.UnitOfWork;
using Netptune.Handlers.Tasks.Commands;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Handlers.Tasks.Commands;

public class MoveTasksToGroupCommandHandlerTests
{
    private readonly Fixture Fixture = new();
    private readonly MoveTasksToGroupCommandHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IEventPublisher EventPublisher = Substitute.For<IEventPublisher>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();
    private readonly IEventRecordWriter EventRecords = Substitute.For<IEventRecordWriter>();
    private readonly ITaskPlacementService Placement = Substitute.For<ITaskPlacementService>();

    public MoveTasksToGroupCommandHandlerTests()
    {
        Identity.GetCurrentUserId().Returns("user-1");
        Identity.GetWorkspaceKey().Returns("workspace");
        Identity.GetWorkspaceId().Returns(1);
        Placement.FollowStatus(default!, default!, default, default).ReturnsForAnyArgs(TaskBoardMoves.None);
        Handler = new(
            UnitOfWork,
            EventPublisher,
            Identity,
            EventRecords,
            Placement);
    }

    private void SetupHandlerDependencies(MoveTasksToGroupRequest request, int? groupStatusId = null)
    {
        UnitOfWork.InvokeTransaction();
        UnitOfWork.BoardGroups.GetTaskTarget(request.NewGroupId!.Value, TestContext.Current.CancellationToken)
            .Returns(new BoardGroupTaskTarget
            {
                Id = request.NewGroupId.Value,
                Name = AutoFixtures.BoardGroup.Name,
                BoardId = 1,
                MaxSortOrder = 7,
                WorkspaceId = 1,
                StatusId = groupStatusId,
            });
        UnitOfWork.Tasks.GetTaskIdsInBoard(request.BoardId, Arg.Any<int>(), TestContext.Current.CancellationToken).Returns(request.TaskIds);
        UnitOfWork.ProjectTasksInGroups
            .GetPlacementGroupsOnBoard(Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<int, int>());
        UnitOfWork.Tasks.GetAllByIdAsync(Arg.Any<IEnumerable<int>>(), true, TestContext.Current.CancellationToken)
            .Returns(request.TaskIds.Select(id => new ProjectTask
            {
                Id = id,
                WorkspaceId = 1,
                OwnerId = "user-1",
                StatusId = 1,
                Status = AutoFixtures.TaskStatus with { Id = 1 },
            }).ToList());
        UnitOfWork.BoardGroups.GetMaxTaskSortOrder(request.NewGroupId.Value, TestContext.Current.CancellationToken).Returns(7D);
        UnitOfWork.Statuses.GetAllByIdAsync(Arg.Any<IEnumerable<int>>(), true, TestContext.Current.CancellationToken)
            .Returns([AutoFixtures.TaskStatus with { Id = 1 }]);
    }

    [Fact]
    public async Task MoveTasksToGroup_ShouldReturnCorrectly_WhenInputValid()
    {
        var request = Fixture.Build<MoveTasksToGroupRequest>().Create();
        SetupHandlerDependencies(request);

        var result = await Handler.Handle(new MoveTasksToGroupCommand(request), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task MoveTasksToGroup_ShouldCallCompleteAsync_WhenInputValid()
    {
        var request = Fixture.Build<MoveTasksToGroupRequest>().Create();
        SetupHandlerDependencies(request);

        await Handler.Handle(new MoveTasksToGroupCommand(request), TestContext.Current.CancellationToken);

        await UnitOfWork.Received(1).CompleteAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task MoveTasksToGroup_ShouldNotUpdateStatuses_WhenGroupHasNoStatus()
    {
        var request = Fixture.Build<MoveTasksToGroupRequest>().Create();
        SetupHandlerDependencies(request, groupStatusId: null);

        await Handler.Handle(new MoveTasksToGroupCommand(request), TestContext.Current.CancellationToken);

        await UnitOfWork.Tasks.DidNotReceive().UpdateTaskStatuses(
            Arg.Any<IEnumerable<int>>(),
            Arg.Any<int>(),
            Arg.Any<CancellationToken>());
        await EventPublisher.DidNotReceive().Dispatch(Arg.Any<TaskChangedMessage>());
    }

    [Fact]
    public async Task MoveTasksToGroup_ShouldUpdateStatusesAndPublish_WhenGroupHasStatus()
    {
        var request = Fixture.Build<MoveTasksToGroupRequest>().Create();
        SetupHandlerDependencies(request, groupStatusId: 5);

        await Handler.Handle(new MoveTasksToGroupCommand(request), TestContext.Current.CancellationToken);

        await UnitOfWork.Tasks.Received(1).UpdateTaskStatuses(
            Arg.Any<IEnumerable<int>>(),
            5,
            TestContext.Current.CancellationToken);
        await EventPublisher.Received(request.TaskIds.Count).Dispatch(Arg.Is<TaskChangedMessage>(message =>
            message.Changes.Any(change =>
                change.Field == TaskChangeField.Status &&
                change.NewValue == "5")));
    }

    [Fact]
    public async Task MoveTasksToGroup_ShouldDispatchSearchIndexEvent_WhenGroupHasStatus()
    {
        var request = Fixture.Build<MoveTasksToGroupRequest>().Create();
        SetupHandlerDependencies(request, groupStatusId: 5);

        await Handler.Handle(new MoveTasksToGroupCommand(request), TestContext.Current.CancellationToken);

        await EventPublisher.Received(1).Dispatch(Arg.Is<SearchIndexEvent>(searchEvent =>
            searchEvent.Operation == SearchIndexOperation.Index &&
            searchEvent.EntityType == "task" &&
            searchEvent.WorkspaceSlug == "workspace" &&
            searchEvent.EntityIds.SequenceEqual(request.TaskIds)));
    }

    [Fact]
    public async Task MoveTasksToGroup_ShouldNotDispatchSearchIndexEvent_WhenGroupHasNoStatus()
    {
        var request = Fixture.Build<MoveTasksToGroupRequest>().Create();
        SetupHandlerDependencies(request, groupStatusId: null);

        await Handler.Handle(new MoveTasksToGroupCommand(request), TestContext.Current.CancellationToken);

        await EventPublisher.DidNotReceive().Dispatch(Arg.Any<SearchIndexEvent>());
    }

    [Fact]
    public async Task MoveTasksToGroup_ShouldReassignBoardGroupMembership_WhenInputValid()
    {
        var request = Fixture.Build<MoveTasksToGroupRequest>().Create();
        SetupHandlerDependencies(request);

        await Handler.Handle(new MoveTasksToGroupCommand(request), TestContext.Current.CancellationToken);

        await Placement.Received(1).PlaceMany(
            Arg.Any<IReadOnlyList<int>>(),
            Arg.Is<BoardGroupTaskTarget>(target => target.Id == request.NewGroupId!.Value),
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task MoveTasksToGroup_ShouldFailWithoutMoving_WhenATaskIsNotOnTheBoard()
    {
        var request = Fixture.Build<MoveTasksToGroupRequest>().Create();
        SetupHandlerDependencies(request);
        UnitOfWork.Tasks.GetTaskIdsInBoard(request.BoardId, Arg.Any<int>(), TestContext.Current.CancellationToken)
            .Returns(request.TaskIds.Skip(1).ToList());

        var result = await Handler.Handle(new MoveTasksToGroupCommand(request), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        await Placement.DidNotReceive().PlaceMany(
            Arg.Any<IReadOnlyList<int>>(),
            Arg.Any<BoardGroupTaskTarget>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MoveTasksToGroup_ShouldLeavePlacementsOnOtherBoardsIntact_WhenInputValid()
    {
        var request = Fixture.Build<MoveTasksToGroupRequest>().Create();
        SetupHandlerDependencies(request);

        await Handler.Handle(new MoveTasksToGroupCommand(request), TestContext.Current.CancellationToken);

        await UnitOfWork.ProjectTasksInGroups.DidNotReceive().DeleteAllByTaskId(
            Arg.Any<IEnumerable<int>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MoveTasksToGroup_ShouldRecordAMovePerTask_UnderTheStatusChangeCorrelation()
    {
        var request = Fixture.Build<MoveTasksToGroupRequest>().Create();
        var recordedMoves = new List<TaskBoardMoves>();
        TaskBoardMoveContext? recordedContext = null;
        IReadOnlyList<IEventWriteRequest> transitions = [];

        SetupHandlerDependencies(request, groupStatusId: 5);
        UnitOfWork.Statuses
            .GetInWorkspace(5, 1, Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(AutoFixtures.TaskStatus with { Id = 5 });
        await EventRecords.AppendRange(
            Arg.Do<IReadOnlyList<IEventWriteRequest>>(requests => transitions = requests),
            Arg.Any<CancellationToken>());
        await Placement.RecordMoves(
            Arg.Do<TaskBoardMoves>(recordedMoves.Add),
            Arg.Do<TaskBoardMoveContext>(context => recordedContext = context),
            Arg.Any<CancellationToken>());

        await Handler.Handle(new MoveTasksToGroupCommand(request), TestContext.Current.CancellationToken);

        var groupMoves = recordedMoves.Should().ContainSingle(moves => moves.HasMoves).Subject;

        groupMoves.Moves.Select(move => move.TaskId).Should().BeEquivalentTo(request.TaskIds);
        groupMoves.Moves.Should().OnlyContain(move => move.Target.Id == request.NewGroupId);
        groupMoves.CausedByStatus.Should().BeNull();
        transitions.Should().HaveCount(request.TaskIds.Count);
        transitions.Should().OnlyContain(transition => transition.CorrelationId == recordedContext!.CorrelationId);
    }

    [Fact]
    public async Task MoveTasksToGroup_ShouldMoveCardsOnOtherBoards_ForTasksWhoseStatusChanged()
    {
        var request = Fixture.Build<MoveTasksToGroupRequest>().Create();
        var newStatus = AutoFixtures.TaskStatus with { Id = 5 };
        var alreadyCompleteId = request.TaskIds[0];
        var changedTaskIds = request.TaskIds.Skip(1).Order().ToList();

        SetupHandlerDependencies(request, groupStatusId: 5);
        UnitOfWork.Statuses
            .GetInWorkspace(5, 1, Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(newStatus);
        UnitOfWork.Tasks.GetAllByIdAsync(Arg.Any<IEnumerable<int>>(), true, TestContext.Current.CancellationToken)
            .Returns(request.TaskIds.Select(id => TaskWithStatus(id, id == alreadyCompleteId ? 5 : 1)).ToList());

        await Handler.Handle(new MoveTasksToGroupCommand(request), TestContext.Current.CancellationToken);

        await Placement.Received(1).FollowStatus(
            Arg.Is<IReadOnlyCollection<int>>(ids => ids.Order().SequenceEqual(changedTaskIds)),
            newStatus,
            1,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MoveTasksToGroup_ShouldLeaveOtherBoards_WhenTheGroupHasNoStatus()
    {
        var request = Fixture.Build<MoveTasksToGroupRequest>().Create();
        SetupHandlerDependencies(request);

        await Handler.Handle(new MoveTasksToGroupCommand(request), TestContext.Current.CancellationToken);

        await Placement.DidNotReceiveWithAnyArgs().FollowStatus(default!, default!, default, default);
    }

    private static ProjectTask TaskWithStatus(int taskId, int statusId)
    {
        return new ProjectTask
        {
            Id = taskId,
            WorkspaceId = 1,
            OwnerId = "user-1",
            StatusId = statusId,
            Status = AutoFixtures.TaskStatus with { Id = statusId },
        };
    }
}
