using AutoFixture;

using FluentAssertions;

using Netptune.Core.Entities;
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

public class MoveTasksToGroupCommandHandlerTests
{
    private readonly Fixture Fixture = new();
    private readonly MoveTasksToGroupCommandHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IActivityLogger Activity = Substitute.For<IActivityLogger>();
    private readonly IEventPublisher EventPublisher = Substitute.For<IEventPublisher>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();
    private readonly IEventRecordWriter EventRecords = Substitute.For<IEventRecordWriter>();

    public MoveTasksToGroupCommandHandlerTests()
    {
        Identity.GetCurrentUserId().Returns("user-1");
        Handler = new(
            UnitOfWork,
            Activity,
            EventPublisher,
            Identity,
            EventRecords);
    }

    private void SetupHandlerDependencies(MoveTasksToGroupRequest request, int? groupStatusId = null)
    {
        UnitOfWork.InvokeTransaction();
        UnitOfWork.BoardGroups.GetTaskTarget(request.NewGroupId!.Value, TestContext.Current.CancellationToken)
            .Returns(new BoardGroupTaskTarget(
                request.NewGroupId.Value,
                AutoFixtures.BoardGroup.Name,
                7,
                1,
                groupStatusId));
        UnitOfWork.Tasks.GetTaskIdsInBoard(request.BoardId, TestContext.Current.CancellationToken).Returns(request.TaskIds);
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
    public async Task MoveTasksToGroup_ShouldReassignBoardGroupMembership_WhenInputValid()
    {
        var request = Fixture.Build<MoveTasksToGroupRequest>().Create();
        SetupHandlerDependencies(request);

        await Handler.Handle(new MoveTasksToGroupCommand(request), TestContext.Current.CancellationToken);

        await UnitOfWork.ProjectTasksInGroups.Received(1).DeleteAllByTaskId(
            Arg.Any<IEnumerable<int>>(),
            TestContext.Current.CancellationToken);
        await UnitOfWork.ProjectTasksInGroups.Received(1).AddRangeAsync(
            Arg.Any<IEnumerable<ProjectTaskInBoardGroup>>(),
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task MoveTasksToGroup_ShouldLogActivity_WhenValidId()
    {
        var request = Fixture.Build<MoveTasksToGroupRequest>().Create();
        SetupHandlerDependencies(request);

        await Handler.Handle(new MoveTasksToGroupCommand(request), TestContext.Current.CancellationToken);

        Activity.Received(1).LogWithMany(Arg.Any<Action<ActivityMultipleOptions<MoveTaskActivityMeta>>>());
    }
}
