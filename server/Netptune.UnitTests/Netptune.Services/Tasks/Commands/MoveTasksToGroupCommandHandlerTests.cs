using AutoFixture;

using FluentAssertions;

using Netptune.Core.Events.Tasks;
using Netptune.Core.Models.Activity;
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

    [Fact]
    public async Task MoveTasksToGroup_ShouldReturnCorrectly_WhenInputValid()
    {
        var request = Fixture.Build<MoveTasksToGroupRequest>().Create();
        UnitOfWork.BoardGroups.GetAsync(request.NewGroupId!.Value, cancellationToken: TestContext.Current.CancellationToken).Returns(AutoFixtures.BoardGroup);
        UnitOfWork.Tasks.GetTaskIdsInBoard(request.BoardId, TestContext.Current.CancellationToken).Returns(new List<int>());

        var result = await Handler.Handle(new MoveTasksToGroupCommand(request), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task MoveTasksToGroup_ShouldCallCompleteAsync_WhenInputValid()
    {
        var request = Fixture.Build<MoveTasksToGroupRequest>().Create();
        UnitOfWork.BoardGroups.GetAsync(request.NewGroupId!.Value, cancellationToken: TestContext.Current.CancellationToken).Returns(AutoFixtures.BoardGroup);
        UnitOfWork.Tasks.GetTaskIdsInBoard(request.BoardId, TestContext.Current.CancellationToken).Returns(new List<int>());

        await Handler.Handle(new MoveTasksToGroupCommand(request), CancellationToken.None);

        await UnitOfWork.Received(1).CompleteAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task MoveTasksToGroup_ShouldLogActivity_WhenValidId()
    {
        var request = Fixture.Build<MoveTasksToGroupRequest>().Create();
        UnitOfWork.BoardGroups.GetAsync(request.NewGroupId!.Value, cancellationToken: TestContext.Current.CancellationToken).Returns(AutoFixtures.BoardGroup);
        UnitOfWork.Tasks.GetTaskIdsInBoard(request.BoardId, TestContext.Current.CancellationToken).Returns(new List<int>());

        await Handler.Handle(new MoveTasksToGroupCommand(request), CancellationToken.None);

        Activity.Received(1).LogWithMany(Arg.Any<Action<ActivityMultipleOptions<MoveTaskActivityMeta>>>());
    }
}
