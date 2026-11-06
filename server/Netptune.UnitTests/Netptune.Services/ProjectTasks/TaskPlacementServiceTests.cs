using FluentAssertions;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Events;
using Netptune.Core.Events.Tasks;
using Netptune.Core.Models.ProjectTasks;
using Netptune.Core.Relationships;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Services.ProjectTasks;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Services.ProjectTasks;

public class TaskPlacementServiceTests
{
    private const int WorkspaceId = 1;

    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IEventRecordWriter EventRecords = Substitute.For<IEventRecordWriter>();
    private readonly TaskPlacementService Service;
    private readonly Status Complete = new() { Id = 7, Name = "Complete" };
    private readonly List<ProjectTaskInBoardGroup> AddedPlacements = [];

    public TaskPlacementServiceTests()
    {
        Service = new TaskPlacementService(UnitOfWork, EventRecords);

        UnitOfWork.ProjectTasksInGroups
            .AddRangeAsync(Arg.Do<IEnumerable<ProjectTaskInBoardGroup>>(AddedPlacements.AddRange), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task FollowStatus_ShouldMoveCardsOnEveryBoard_WithOneTargetLookupAndOneDelete()
    {
        var firstDone = Target(101, 10, maxSortOrder: 4);
        var secondDone = Target(201, 20, maxSortOrder: 0);

        SetupPlacements(
            Placement(1, taskId: 1, boardId: 10, groupId: 100),
            Placement(2, taskId: 2, boardId: 10, groupId: 100),
            Placement(3, taskId: 1, boardId: 20, groupId: 200));
        SetupTargets(firstDone, secondDone);

        var moves = await Service.FollowStatus([1, 2], Complete, cancellationToken: TestContext.Current.CancellationToken);

        moves.CausedByStatus.Should().Be(Complete);
        moves.Moves.Should().BeEquivalentTo(new[]
        {
            new TaskBoardMove(1, 100, firstDone),
            new TaskBoardMove(2, 100, firstDone),
            new TaskBoardMove(1, 200, secondDone),
        });

        await UnitOfWork.BoardGroups.Received(1).GetStatusTaskTargets(
            Arg.Is<IReadOnlyCollection<int>>(ids => ids.Order().SequenceEqual(new[] { 10, 20 })),
            Complete.Id,
            Arg.Any<CancellationToken>());
        await UnitOfWork.ProjectTasksInGroups.Received(1).DeletePlacements(
            Arg.Is<IReadOnlyCollection<int>>(ids => ids.Order().SequenceEqual(new[] { 1, 2, 3 })),
            Arg.Any<CancellationToken>());

        AddedPlacements.Select(placement => (placement.ProjectTaskId, placement.BoardGroupId, placement.SortOrder))
            .Should()
            .BeEquivalentTo(new[] { (1, 101, 5D), (2, 101, 6D), (1, 201, 1D) });
    }

    [Fact]
    public async Task FollowStatus_ShouldLeaveTheCard_WhenItAlreadySitsInAColumnMappedToTheStatus()
    {
        SetupPlacements(Placement(1, taskId: 1, boardId: 10, groupId: 102, groupStatusId: Complete.Id));

        var moves = await Service.FollowStatus([1], Complete, cancellationToken: TestContext.Current.CancellationToken);

        moves.HasMoves.Should().BeFalse();

        await UnitOfWork.BoardGroups.DidNotReceiveWithAnyArgs().GetStatusTaskTargets(default!, default, default);
        await UnitOfWork.ProjectTasksInGroups.DidNotReceiveWithAnyArgs().DeletePlacements(default!, default);
    }

    [Fact]
    public async Task FollowStatus_ShouldLeaveTheCard_WhenItsBoardHasNoColumnForTheStatus()
    {
        SetupPlacements(Placement(1, taskId: 1, boardId: 10, groupId: 100));
        SetupTargets();

        var moves = await Service.FollowStatus([1], Complete, cancellationToken: TestContext.Current.CancellationToken);

        moves.HasMoves.Should().BeFalse();

        await UnitOfWork.ProjectTasksInGroups.DidNotReceiveWithAnyArgs().DeletePlacements(default!, default);
    }

    [Fact]
    public async Task FollowStatus_ShouldLeaveTheSkippedBoard()
    {
        SetupPlacements(
            Placement(1, taskId: 1, boardId: 10, groupId: 100),
            Placement(2, taskId: 1, boardId: 20, groupId: 200));
        SetupTargets(Target(101, 10, maxSortOrder: 0), Target(201, 20, maxSortOrder: 0));

        var moves = await Service.FollowStatus([1], Complete, 10, TestContext.Current.CancellationToken);

        moves.Moves.Should().ContainSingle().Which.Target.BoardId.Should().Be(20);
    }

    [Fact]
    public async Task RecordMoves_ShouldAppendOneRegisteredBoardMoveEventPerMove()
    {
        var done = Target(101, 10, maxSortOrder: 0);
        var moves = new TaskBoardMoves(Complete, [new TaskBoardMove(1, 100, done), new TaskBoardMove(2, null, done)]);
        var context = new TaskBoardMoveContext
        {
            WorkspaceId = WorkspaceId,
            CorrelationId = Guid.NewGuid(),
            ActorUserId = "automation-user",
            OriginType = EventOriginType.Automation,
        };
        IReadOnlyList<IEventWriteRequest> appended = [];

        await EventRecords.AppendRange(
            Arg.Do<IReadOnlyList<IEventWriteRequest>>(requests => appended = requests),
            Arg.Any<CancellationToken>());

        await Service.RecordMoves(moves, context, TestContext.Current.CancellationToken);

        appended.Should().HaveCount(2);
        appended.Should().AllSatisfy(request =>
        {
            var validate = () => EventDefinitionRegistry.Validate(request);

            validate.Should().NotThrow();
            request.EventKey.Should().Be(EventKeys.TaskBoardMoved);
            request.WorkspaceId.Should().Be(WorkspaceId);
            request.CorrelationId.Should().Be(context.CorrelationId);
            request.ActorUserId.Should().Be("automation-user");
            request.OriginType.Should().Be(EventOriginType.Automation);
        });

        var moved = (MoveTaskActivityMeta)appended[0].Payload;
        var joined = (MoveTaskActivityMeta)appended[1].Payload;

        appended[0].SubjectId.Should().Be("1");
        moved.Group.Should().Be(done.Name);
        moved.GroupId.Should().Be(done.Id);
        moved.FromGroupId.Should().Be(100);
        moved.BoardId.Should().BeNull();
        moved.CausedByStatusId.Should().Be(Complete.Id);
        moved.CausedByStatus.Should().Be(Complete.Name);
        joined.BoardId.Should().Be(done.BoardId);
    }

    [Fact]
    public async Task RecordMoves_ShouldAppendNothing_WhenThereAreNoMoves()
    {
        var context = new TaskBoardMoveContext { WorkspaceId = WorkspaceId, CorrelationId = Guid.NewGuid() };

        await Service.RecordMoves(TaskBoardMoves.None, context, TestContext.Current.CancellationToken);

        await EventRecords.DidNotReceiveWithAnyArgs().AppendRange(default!, default);
    }

    private void SetupPlacements(params TaskBoardPlacement[] placements)
    {
        UnitOfWork.ProjectTasksInGroups
            .GetBoardPlacements(Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<CancellationToken>())
            .Returns([.. placements]);
    }

    private void SetupTargets(params BoardGroupTaskTarget[] targets)
    {
        var targetsByBoard = targets.ToDictionary(target => target.BoardId);

        UnitOfWork.BoardGroups
            .GetStatusTaskTargets(Arg.Any<IReadOnlyCollection<int>>(), Complete.Id, Arg.Any<CancellationToken>())
            .Returns(targetsByBoard);
    }

    private static TaskBoardPlacement Placement(int id, int taskId, int boardId, int groupId, int? groupStatusId = null)
    {
        return new TaskBoardPlacement
        {
            Id = id,
            TaskId = taskId,
            BoardId = boardId,
            BoardGroupId = groupId,
            GroupStatusId = groupStatusId,
        };
    }

    private BoardGroupTaskTarget Target(int id, int boardId, double maxSortOrder)
    {
        return new BoardGroupTaskTarget
        {
            Id = id,
            Name = $"Done {id}",
            MaxSortOrder = maxSortOrder,
            BoardId = boardId,
            StatusId = Complete.Id,
        };
    }
}
