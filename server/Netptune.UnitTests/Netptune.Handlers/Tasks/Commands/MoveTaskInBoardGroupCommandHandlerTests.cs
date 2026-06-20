using FluentAssertions;

using Netptune.Core.Enums;
using Netptune.Core.Events.Tasks;
using Netptune.Core.Models.Activity;
using Netptune.Core.Models.ProjectTasks;
using Netptune.Core.Relationships;
using Netptune.Core.Requests;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
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

    public MoveTaskInBoardGroupCommandHandlerTests()
    {
        Identity.GetCurrentUserId().Returns("user-1");
        Handler = new(UnitOfWork, Activity, EventPublisher, Identity);
    }

    private void SetupTransfer(MoveTaskInGroupRequest request, ProjectTaskInBoardGroup taskInGroup)
    {
        var status = AutoFixtures.TaskStatus with
        {
            Id = 5,
            WorkspaceId = 1,
            Category = AutoFixtures.BoardGroup.Type.GetStatusCategoryFromGroupType(),
        };

        UnitOfWork.InvokeTransaction<BoardGroupTaskTarget>();
        UnitOfWork.BoardGroups.GetTaskTarget(request.NewGroupId, TestContext.Current.CancellationToken)
            .Returns(new BoardGroupTaskTarget(request.NewGroupId, AutoFixtures.BoardGroup.Name, AutoFixtures.BoardGroup.Type, 7, 1));
        UnitOfWork.Statuses.GetFirstTaskStatusByCategory(1, Arg.Any<StatusCategory>(), TestContext.Current.CancellationToken)
            .Returns(status);
        UnitOfWork.Tasks.UpdateTaskStatus(request.TaskId, Arg.Any<int>(), TestContext.Current.CancellationToken).Returns(1);
        UnitOfWork.ProjectTasksInGroups.GetProjectTaskInGroup(request.TaskId, request.NewGroupId, TestContext.Current.CancellationToken).Returns(taskInGroup);
        UnitOfWork.ProjectTasksInGroups.GetNeighborSortOrdersForInsert(
                request.NewGroupId,
                request.TaskId,
                request.CurrentIndex,
                TestContext.Current.CancellationToken)
            .Returns(((double?)0D, (double?)2D));
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

    [Fact]
    public async Task MoveTaskInBoardGroup_ShouldReturnCorrectly_WhenInputValid()
    {
        var request = new MoveTaskInGroupRequest
        {
            CurrentIndex = 1,
            PreviousIndex = 0,
            TaskId = 1,
            NewGroupId = 1,
            OldGroupId = 2,
        };

        var taskInGroupA = AutoFixtures.ProjectTaskInBoardGroup with { ProjectTaskId = request.TaskId };
        SetupTransfer(request, taskInGroupA);

        var result = await Handler.Handle(new MoveTaskInBoardGroupCommand(request), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task MoveTaskInBoardGroup_ShouldCallCompleteAsync_WhenInputValid()
    {
        var request = new MoveTaskInGroupRequest
        {
            CurrentIndex = 1,
            PreviousIndex = 0,
            TaskId = 1,
            NewGroupId = 1,
            OldGroupId = 2,
        };

        var taskInGroupA = AutoFixtures.ProjectTaskInBoardGroup with { ProjectTaskId = request.TaskId };
        SetupTransfer(request, taskInGroupA);

        await Handler.Handle(new MoveTaskInBoardGroupCommand(request), TestContext.Current.CancellationToken);

        await UnitOfWork.Received(2).CompleteAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task MoveTaskInBoardGroup_ShouldLogActivity_WhenValidId()
    {
        var request = new MoveTaskInGroupRequest
        {
            CurrentIndex = 1,
            PreviousIndex = 0,
            TaskId = 1,
            NewGroupId = 1,
            OldGroupId = 2,
        };

        var taskInGroupA = AutoFixtures.ProjectTaskInBoardGroup with { ProjectTaskId = request.TaskId };
        SetupTransfer(request, taskInGroupA);

        await Handler.Handle(new MoveTaskInBoardGroupCommand(request), TestContext.Current.CancellationToken);

        Activity.Received(1).LogWith(Arg.Any<Action<ActivityOptions<MoveTaskActivityMeta>>>());
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
