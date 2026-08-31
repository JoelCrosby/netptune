using FluentAssertions;

using Netptune.Core.Entities;
using Netptune.Core.Events.Tasks;
using Netptune.Core.Models.Activity;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.Services.ProjectTasks;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.ProjectTasks;
using Netptune.Handlers.Tasks.Commands;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Handlers.Tasks.Commands;

public class RemoveTaskFromBoardCommandHandlerTests
{
    private const int WorkspaceId = 1;

    private readonly RemoveTaskFromBoardCommandHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();
    private readonly IActivityLogger Activity = Substitute.For<IActivityLogger>();
    private readonly ITaskPlacementService Placement = Substitute.For<ITaskPlacementService>();

    public RemoveTaskFromBoardCommandHandlerTests()
    {
        Identity.GetWorkspaceId().Returns(WorkspaceId);
        Handler = new(UnitOfWork, Identity, Activity, Placement);
    }

    private (ProjectTask Task, Board Board) Setup(bool removed = true)
    {
        var task = AutoFixtures.ProjectTask;
        var board = AutoFixtures.Board;

        UnitOfWork.Tasks
            .GetInWorkspace(task.Id, WorkspaceId, false, TestContext.Current.CancellationToken)
            .Returns(task);

        UnitOfWork.Boards
            .GetInWorkspace(board.Id, WorkspaceId, false, TestContext.Current.CancellationToken)
            .Returns(board);

        Placement.RemoveFromBoard(task.Id, board.Id, TestContext.Current.CancellationToken).Returns(removed);

        UnitOfWork.Tasks
            .GetTaskViewModel(task.Id, TestContext.Current.CancellationToken)
            .Returns(new TaskViewModel { Id = task.Id, WorkspaceId = WorkspaceId });

        return (task, board);
    }

    [Fact]
    public async Task Handle_ShouldDetachTheTaskFromTheBoard_WhenItIsPlacedOnIt()
    {
        var (task, board) = Setup();

        var result = await Handler.Handle(
            new(task.Id, board.Id),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Payload!.Id.Should().Be(task.Id);

        await Placement.Received(1).RemoveFromBoard(task.Id, board.Id, TestContext.Current.CancellationToken);
        await UnitOfWork.Received(1).CompleteAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Handle_ShouldRecordTheBoardItLeft()
    {
        var (task, board) = Setup();

        await Handler.Handle(new(task.Id, board.Id), TestContext.Current.CancellationToken);

        Activity.Received(1).LogWith(Arg.Any<Action<ActivityOptions<RemoveTaskFromBoardActivityMeta>>>());
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenTheTaskIsNotOnTheBoard()
    {
        var (task, board) = Setup(removed: false);

        var result = await Handler.Handle(
            new(task.Id, board.Id),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("not on board");

        await UnitOfWork.DidNotReceive().CompleteAsync(TestContext.Current.CancellationToken);
        Activity.DidNotReceive().LogWith(Arg.Any<Action<ActivityOptions<RemoveTaskFromBoardActivityMeta>>>());
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenTheBoardIsOutsideTheWorkspace()
    {
        var (task, _) = Setup();

        var result = await Handler.Handle(new(task.Id, 404), TestContext.Current.CancellationToken);

        result.IsNotFound.Should().BeTrue();
    }
}
