using FluentAssertions;

using Netptune.Core.Enums;
using Netptune.Core.Events;
using Netptune.Core.Events.Tasks;
using Netptune.Core.Models.ProjectTasks;
using Netptune.Core.Relationships;
using Netptune.Core.Requests;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.Services.ProjectTasks;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.ProjectTasks;
using Netptune.Handlers.Tasks.Commands;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Handlers.Tasks.Commands;

public class MoveTaskInBoardGroupCommandHandlerTests
{
    private readonly MoveTaskInBoardGroupCommandHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IActivityLogger Activity = Substitute.For<IActivityLogger>();
    private readonly IEventPublisher EventPublisher = Substitute.For<IEventPublisher>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();
    private readonly IEventRecordWriter EventRecords = Substitute.For<IEventRecordWriter>();
    private readonly ITaskPlacementService Placement = Substitute.For<ITaskPlacementService>();

    public MoveTaskInBoardGroupCommandHandlerTests()
    {
        Identity.GetCurrentUserId().Returns("user-1");
        UnitOfWork.Tasks
            .GetInWorkspace(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(AutoFixtures.ProjectTask);
        UnitOfWork.BoardGroups
            .GetInWorkspace(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(AutoFixtures.BoardGroup);
        Placement.FollowStatus(default!, default!, default, default).ReturnsForAnyArgs(TaskBoardMoves.None);
        Handler = new(
            UnitOfWork,
            Activity,
            EventPublisher,
            Identity,
            EventRecords,
            Placement);
    }

    private void SetupTransfer(MoveTaskInGroupRequest request, ProjectTaskInBoardGroup taskInGroup, int? groupStatusId = null)
    {
        UnitOfWork.InvokeTransaction<BoardGroupTaskTarget>();
        UnitOfWork.BoardGroups.GetTaskTarget(request.NewGroupId, TestContext.Current.CancellationToken)
            .Returns(new BoardGroupTaskTarget
            {
                Id = request.NewGroupId,
                Name = AutoFixtures.BoardGroup.Name,
                BoardId = 1,
                MaxSortOrder = 7,
                WorkspaceId = 1,
                StatusId = groupStatusId,
            });
        UnitOfWork.Tasks.GetTaskViewModel(request.TaskId, TestContext.Current.CancellationToken)
            .Returns(new TaskViewModel { Id = request.TaskId, StatusId = 1, WorkspaceId = 1 });
        UnitOfWork.ProjectTasksInGroups.GetProjectTaskInGroup(request.TaskId, request.NewGroupId, TestContext.Current.CancellationToken).Returns(taskInGroup);
        UnitOfWork.ProjectTasksInGroups.GetNeighborSortOrdersForInsert(
            request.NewGroupId,
            request.TaskId,
            request.CurrentIndex,
            TestContext.Current.CancellationToken)
            .Returns(((double?)0D, (double?)2D));
        Placement.Place(request.TaskId, Arg.Any<BoardGroupTaskTarget>(), Arg.Any<CancellationToken>()).Returns(true);
    }

    private void SetupSameGroup(MoveTaskInGroupRequest request, ProjectTaskInBoardGroup taskInGroup)
    {
        UnitOfWork.ProjectTasksInGroups.GetProjectTaskInGroup(request.TaskId, request.NewGroupId, TestContext.Current.CancellationToken).Returns(taskInGroup);
        UnitOfWork.ProjectTasksInGroups.GetNeighborSortOrdersForInsert(
            request.NewGroupId,
            request.TaskId,
            request.CurrentIndex,
            TestContext.Current.CancellationToken)
            .Returns(((double?)0D, (double?)2D));
    }

    private static MoveTaskInGroupRequest TransferRequest() => new()
    {
        CurrentIndex = 1,
        PreviousIndex = 0,
        TaskId = 1,
        NewGroupId = 1,
        OldGroupId = 2,
    };

    [Fact]
    public async Task MoveTaskInBoardGroup_ShouldReturnCorrectly_WhenInputValid()
    {
        var request = TransferRequest();
        var taskInGroupA = AutoFixtures.ProjectTaskInBoardGroup with { ProjectTaskId = request.TaskId };
        SetupTransfer(request, taskInGroupA);

        var result = await Handler.Handle(new MoveTaskInBoardGroupCommand(request), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task MoveTaskInBoardGroup_ShouldNotChangeStatus_WhenGroupHasNoStatus()
    {
        var request = TransferRequest();
        var taskInGroupA = AutoFixtures.ProjectTaskInBoardGroup with { ProjectTaskId = request.TaskId };
        SetupTransfer(request, taskInGroupA, groupStatusId: null);

        await Handler.Handle(new MoveTaskInBoardGroupCommand(request), TestContext.Current.CancellationToken);

        await UnitOfWork.Tasks.DidNotReceive().UpdateTaskStatus(
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<CancellationToken>());
        await EventPublisher.DidNotReceive().Dispatch(Arg.Any<TaskChangedMessage>());
    }

    [Fact]
    public async Task MoveTaskInBoardGroup_ShouldApplyStatusAndPublish_WhenGroupHasStatus()
    {
        var request = TransferRequest();
        var taskInGroupA = AutoFixtures.ProjectTaskInBoardGroup with { ProjectTaskId = request.TaskId };
        SetupTransfer(request, taskInGroupA, groupStatusId: 5);

        await Handler.Handle(new MoveTaskInBoardGroupCommand(request), TestContext.Current.CancellationToken);

        await UnitOfWork.Tasks.Received(1).UpdateTaskStatus(request.TaskId, 5, TestContext.Current.CancellationToken);
        await EventPublisher.Received(1).Dispatch(Arg.Is<TaskChangedMessage>(message =>
            message.TaskId == request.TaskId &&
            message.WorkspaceId == 1 &&
            message.Changes.Any(change =>
                change.Field == TaskChangeField.Status &&
                change.OldValue == "1" &&
                change.NewValue == "5")));
    }

    [Fact]
    public async Task MoveTaskInBoardGroup_ShouldCallCompleteAsync_WhenInputValid()
    {
        var request = TransferRequest();
        var taskInGroupA = AutoFixtures.ProjectTaskInBoardGroup with { ProjectTaskId = request.TaskId };
        SetupTransfer(request, taskInGroupA);

        await Handler.Handle(new MoveTaskInBoardGroupCommand(request), TestContext.Current.CancellationToken);

        await UnitOfWork.Received(2).CompleteAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task MoveTaskInBoardGroup_ShouldRecordTheMove_UnderTheStatusChangeCorrelation()
    {
        var request = TransferRequest();
        var taskInGroupA = AutoFixtures.ProjectTaskInBoardGroup with { ProjectTaskId = request.TaskId };
        var recordedMoves = new List<TaskBoardMoves>();
        TaskBoardMoveContext? recordedContext = null;
        Guid? statusCorrelationId = null;

        SetupTransfer(request, taskInGroupA, groupStatusId: 5);
        Identity.GetWorkspaceId().Returns(1);
        UnitOfWork.Statuses
            .GetInWorkspace(5, 1, Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(AutoFixtures.TaskStatus with { Id = 5 });
        await EventRecords.Append(
            Arg.Do<EventWriteRequest<FieldTransitionedPayload>>(transition => statusCorrelationId = transition.CorrelationId),
            Arg.Any<CancellationToken>());
        await Placement.RecordMoves(
            Arg.Do<TaskBoardMoves>(recordedMoves.Add),
            Arg.Do<TaskBoardMoveContext>(context => recordedContext = context),
            Arg.Any<CancellationToken>());

        await Handler.Handle(new MoveTaskInBoardGroupCommand(request), TestContext.Current.CancellationToken);

        var dragMoves = recordedMoves.Should().ContainSingle(moves => moves.HasMoves).Subject;
        var move = dragMoves.Moves.Should().ContainSingle().Subject;

        move.TaskId.Should().Be(request.TaskId);
        move.FromGroupId.Should().Be(request.OldGroupId);
        move.Target.Id.Should().Be(request.NewGroupId);
        dragMoves.CausedByStatus.Should().BeNull();
        recordedContext!.WorkspaceId.Should().Be(1);
        statusCorrelationId.Should().Be(recordedContext.CorrelationId);
        Activity.DidNotReceiveWithAnyArgs().LogWith<MoveTaskActivityMeta>(default!);
    }

    [Fact]
    public async Task MoveTaskInBoardGroup_ShouldRecordNoMove_WhenTheCardIsAlreadyInTheGroup()
    {
        var request = TransferRequest();
        var taskInGroupA = AutoFixtures.ProjectTaskInBoardGroup with { ProjectTaskId = request.TaskId };

        SetupTransfer(request, taskInGroupA);
        Placement.Place(request.TaskId, Arg.Any<BoardGroupTaskTarget>(), Arg.Any<CancellationToken>()).Returns(false);

        await Handler.Handle(new MoveTaskInBoardGroupCommand(request), TestContext.Current.CancellationToken);

        await Placement.DidNotReceive().RecordMoves(
            Arg.Is<TaskBoardMoves>(moves => moves.HasMoves),
            Arg.Any<TaskBoardMoveContext>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MoveTaskInBoardGroup_ShouldMoveCardsOnOtherBoards_WhenTheStatusChanges()
    {
        var request = TransferRequest();
        var taskInGroupA = AutoFixtures.ProjectTaskInBoardGroup with { ProjectTaskId = request.TaskId };
        var otherBoardDone = new BoardGroupTaskTarget { Id = 40, Name = "Done", MaxSortOrder = 0, BoardId = 4 };
        var newStatus = AutoFixtures.TaskStatus with { Id = 5 };
        var statusMoves = new TaskBoardMoves(newStatus, [new TaskBoardMove(request.TaskId, 41, otherBoardDone)]);

        SetupTransfer(request, taskInGroupA, groupStatusId: 5);
        UnitOfWork.Statuses
            .GetInWorkspace(5, 1, Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(newStatus);
        Placement
            .FollowStatus(Arg.Any<IReadOnlyCollection<int>>(), newStatus, 1, Arg.Any<CancellationToken>())
            .Returns(statusMoves);

        await Handler.Handle(new MoveTaskInBoardGroupCommand(request), TestContext.Current.CancellationToken);

        await Placement.Received(1).FollowStatus(
            Arg.Is<IReadOnlyCollection<int>>(ids => ids.SequenceEqual(new[] { request.TaskId })),
            newStatus,
            1,
            Arg.Any<CancellationToken>());
        await Placement.Received(1).RecordMoves(statusMoves, Arg.Any<TaskBoardMoveContext>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MoveTaskInBoardGroup_ShouldLeaveOtherBoards_WhenTheStatusIsUnchanged()
    {
        var request = TransferRequest();
        var taskInGroupA = AutoFixtures.ProjectTaskInBoardGroup with { ProjectTaskId = request.TaskId };

        SetupTransfer(request, taskInGroupA, groupStatusId: 1);

        await Handler.Handle(new MoveTaskInBoardGroupCommand(request), TestContext.Current.CancellationToken);

        await Placement.DidNotReceiveWithAnyArgs().FollowStatus(default!, default!, default, default);
    }

    [Fact]
    public async Task MoveTaskInBoardGroup_SameGroup_ShouldReturnCorrectly_WhenInputValid()
    {
        var request = new MoveTaskInGroupRequest
        {
            CurrentIndex = 1,
            PreviousIndex = 0,
            TaskId = 1,
            NewGroupId = 1,
            OldGroupId = 1,
        };

        var taskInGroupA = AutoFixtures.ProjectTaskInBoardGroup with { ProjectTaskId = request.TaskId };
        SetupSameGroup(request, taskInGroupA);

        var result = await Handler.Handle(new MoveTaskInBoardGroupCommand(request), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
    }
}
