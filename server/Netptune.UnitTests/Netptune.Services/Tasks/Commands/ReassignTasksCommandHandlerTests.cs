using AutoFixture;

using FluentAssertions;

using Netptune.Core.Events.Tasks;
using Netptune.Core.Models.Activity;
using Netptune.Core.Requests;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Services.Tasks.Commands.ReassignTasks;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Services.Tasks.Commands;

public class ReassignTasksCommandHandlerTests
{
    private readonly Fixture Fixture = new();
    private readonly ReassignTasksCommandHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IActivityLogger Activity = Substitute.For<IActivityLogger>();

    public ReassignTasksCommandHandlerTests()
    {
        Handler = new(UnitOfWork, Activity);
    }

    [Fact]
    public async Task ReassignTasks_ShouldReturnCorrectly_WhenInputValid()
    {
        var request = Fixture.Build<ReassignTasksRequest>().Create();
        UnitOfWork.Tasks.GetTaskIdsInBoard(request.BoardId).Returns(new List<int>());
        UnitOfWork.Tasks.GetAllByIdAsync(Arg.Any<IEnumerable<int>>()).Returns(AutoFixtures.ProjectTasks);

        var result = await Handler.Handle(new ReassignTasksCommand(request), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ReassignTasks_ShouldCallCompleteAsync_WhenInputValid()
    {
        var request = Fixture.Build<ReassignTasksRequest>().Create();
        UnitOfWork.Tasks.GetTaskIdsInBoard(request.BoardId).Returns(new List<int>());
        UnitOfWork.Tasks.GetAllByIdAsync(Arg.Any<IEnumerable<int>>()).Returns(AutoFixtures.ProjectTasks);

        await Handler.Handle(new ReassignTasksCommand(request), CancellationToken.None);

        await UnitOfWork.Received(1).CompleteAsync();
    }

    [Fact]
    public async Task ReassignTasks_ShouldLogActivity_WhenValidId()
    {
        var request = Fixture.Build<ReassignTasksRequest>().Create();
        UnitOfWork.Tasks.GetTaskIdsInBoard(request.BoardId).Returns(new List<int>());
        UnitOfWork.Tasks.GetAllByIdAsync(Arg.Any<IEnumerable<int>>()).Returns(AutoFixtures.ProjectTasks);

        await Handler.Handle(new ReassignTasksCommand(request), CancellationToken.None);

        Activity.Received(1).LogWithMany(Arg.Any<Action<ActivityMultipleOptions<AssignActivityMeta>>>());
    }
}
