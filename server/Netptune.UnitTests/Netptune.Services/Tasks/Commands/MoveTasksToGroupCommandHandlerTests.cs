using AutoFixture;

using FluentAssertions;

using Netptune.Core.Events.Tasks;
using Netptune.Core.Models.Activity;
using Netptune.Core.Models.ProjectTasks;
using Netptune.Core.Requests;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Services.Tasks.Commands;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Services.Tasks.Commands;

public class MoveTasksToGroupCommandHandlerTests
{
    private readonly Fixture Fixture = new();
    private readonly MoveTasksToGroupCommandHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IActivityLogger Activity = Substitute.For<IActivityLogger>();

    public MoveTasksToGroupCommandHandlerTests()
    {
        Handler = new(UnitOfWork, Activity);
    }

    private void SetupHandlerDependencies(MoveTasksToGroupRequest request)
    {
        UnitOfWork.InvokeTransaction();
        UnitOfWork.BoardGroups.GetTaskTarget(request.NewGroupId!.Value, TestContext.Current.CancellationToken)
            .Returns(new BoardGroupTaskTarget(request.NewGroupId.Value, AutoFixtures.BoardGroup.Name, AutoFixtures.BoardGroup.Type, 7));
        UnitOfWork.Tasks.GetTaskIdsInBoard(request.BoardId, TestContext.Current.CancellationToken).Returns(request.TaskIds);
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
    public async Task MoveTasksToGroup_ShouldLogActivity_WhenValidId()
    {
        var request = Fixture.Build<MoveTasksToGroupRequest>().Create();
        SetupHandlerDependencies(request);

        await Handler.Handle(new MoveTasksToGroupCommand(request), TestContext.Current.CancellationToken);

        Activity.Received(1).LogWithMany(Arg.Any<Action<ActivityMultipleOptions<MoveTaskActivityMeta>>>());
    }
}
