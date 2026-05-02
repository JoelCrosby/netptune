using FluentAssertions;

using Netptune.Core.Entities;
using Netptune.Core.Events.Tasks;
using Netptune.Core.Models.Activity;
using Netptune.Core.Relationships;
using Netptune.Core.Requests;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Services.Tasks.Commands;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Services.Tasks.Commands;

public class MoveTaskInBoardGroupCommandHandlerTests
{
    private readonly MoveTaskInBoardGroupCommandHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IActivityLogger Activity = Substitute.For<IActivityLogger>();

    public MoveTaskInBoardGroupCommandHandlerTests()
    {
        Handler = new(UnitOfWork, Activity);
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
        var taskInGroupB = AutoFixtures.ProjectTaskInBoardGroup;
        var taskInGroups = new List<ProjectTaskInBoardGroup> { taskInGroupA, taskInGroupB };

        UnitOfWork.InvokeTransaction<BoardGroup>();
        UnitOfWork.ProjectTasksInGroups.GetProjectTaskInGroup(request.TaskId, request.NewGroupId, TestContext.Current.CancellationToken).Returns(taskInGroupA);
        UnitOfWork.ProjectTasksInGroups.GetProjectTasksInGroup(Arg.Any<int>(), TestContext.Current.CancellationToken).Returns(taskInGroups);
        UnitOfWork.BoardGroups.GetTasksInGroup(request.NewGroupId, cancellationToken: TestContext.Current.CancellationToken).Returns(AutoFixtures.ProjectTasks);
        UnitOfWork.BoardGroups.GetAsync(request.NewGroupId, cancellationToken: TestContext.Current.CancellationToken).Returns(AutoFixtures.BoardGroup);
        UnitOfWork.Tasks.GetAsync(request.TaskId, cancellationToken: TestContext.Current.CancellationToken).Returns(AutoFixtures.ProjectTask);

        var result = await Handler.Handle(new MoveTaskInBoardGroupCommand(request), CancellationToken.None);

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
        var taskInGroupB = AutoFixtures.ProjectTaskInBoardGroup;
        var taskInGroups = new List<ProjectTaskInBoardGroup> { taskInGroupA, taskInGroupB };

        UnitOfWork.InvokeTransaction<BoardGroup>();
        UnitOfWork.ProjectTasksInGroups.GetProjectTaskInGroup(request.TaskId, request.NewGroupId, TestContext.Current.CancellationToken).Returns(taskInGroupA);
        UnitOfWork.ProjectTasksInGroups.GetProjectTasksInGroup(Arg.Any<int>(), TestContext.Current.CancellationToken).Returns(taskInGroups);
        UnitOfWork.BoardGroups.GetTasksInGroup(request.NewGroupId, cancellationToken: TestContext.Current.CancellationToken).Returns(AutoFixtures.ProjectTasks);
        UnitOfWork.BoardGroups.GetAsync(request.NewGroupId, cancellationToken: TestContext.Current.CancellationToken).Returns(AutoFixtures.BoardGroup);
        UnitOfWork.Tasks.GetAsync(request.TaskId, cancellationToken: TestContext.Current.CancellationToken).Returns(AutoFixtures.ProjectTask);

        await Handler.Handle(new MoveTaskInBoardGroupCommand(request), CancellationToken.None);

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
        var taskInGroupB = AutoFixtures.ProjectTaskInBoardGroup;
        var taskInGroups = new List<ProjectTaskInBoardGroup> { taskInGroupA, taskInGroupB };

        UnitOfWork.InvokeTransaction<BoardGroup>();
        UnitOfWork.ProjectTasksInGroups.GetProjectTaskInGroup(request.TaskId, request.NewGroupId, TestContext.Current.CancellationToken).Returns(taskInGroupA);
        UnitOfWork.ProjectTasksInGroups.GetProjectTasksInGroup(Arg.Any<int>(), TestContext.Current.CancellationToken).Returns(taskInGroups);
        UnitOfWork.BoardGroups.GetTasksInGroup(request.NewGroupId, cancellationToken: TestContext.Current.CancellationToken).Returns(AutoFixtures.ProjectTasks);
        UnitOfWork.BoardGroups.GetAsync(request.NewGroupId, cancellationToken: TestContext.Current.CancellationToken).Returns(AutoFixtures.BoardGroup);
        UnitOfWork.Tasks.GetAsync(request.TaskId, cancellationToken: TestContext.Current.CancellationToken).Returns(AutoFixtures.ProjectTask);

        await Handler.Handle(new MoveTaskInBoardGroupCommand(request), CancellationToken.None);

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
        var taskInGroupB = AutoFixtures.ProjectTaskInBoardGroup;
        var taskInGroups = new List<ProjectTaskInBoardGroup> { taskInGroupA, taskInGroupB };

        UnitOfWork.InvokeTransaction<BoardGroup>();
        UnitOfWork.ProjectTasksInGroups.GetProjectTaskInGroup(request.TaskId, request.NewGroupId, TestContext.Current.CancellationToken).Returns(taskInGroupA);
        UnitOfWork.ProjectTasksInGroups.GetProjectTasksInGroup(Arg.Any<int>(), TestContext.Current.CancellationToken).Returns(taskInGroups);
        UnitOfWork.BoardGroups.GetTasksInGroup(request.NewGroupId, cancellationToken: TestContext.Current.CancellationToken).Returns(AutoFixtures.ProjectTasks);
        UnitOfWork.BoardGroups.GetAsync(request.NewGroupId, cancellationToken: TestContext.Current.CancellationToken).Returns(AutoFixtures.BoardGroup);
        UnitOfWork.Tasks.GetAsync(request.TaskId, cancellationToken: TestContext.Current.CancellationToken).Returns(AutoFixtures.ProjectTask);

        var result = await Handler.Handle(new MoveTaskInBoardGroupCommand(request), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }
}
