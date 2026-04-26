using FluentAssertions;

using Netptune.Core.Models.Activity;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Services.Tasks.Commands.DeleteTask;

using NSubstitute;
using NSubstitute.ReturnsExtensions;

using Xunit;

namespace Netptune.UnitTests.Netptune.Services.Tasks.Commands;

public class DeleteTaskCommandHandlerTests
{
    private readonly DeleteTaskCommandHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IActivityLogger Activity = Substitute.For<IActivityLogger>();

    public DeleteTaskCommandHandlerTests()
    {
        Handler = new(UnitOfWork, Activity);
    }

    [Fact]
    public async Task Delete_ShouldReturnSuccess_WhenValidId()
    {
        UnitOfWork.Tasks.GetAsync(1).Returns(AutoFixtures.ProjectTask);

        var result = await Handler.Handle(new DeleteTaskCommand(1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Delete_ShouldCallDeletePermanent_WhenValidId()
    {
        var taskToDelete = AutoFixtures.ProjectTask;
        UnitOfWork.Tasks.GetAsync(Arg.Any<int>()).Returns(taskToDelete);

        await Handler.Handle(new DeleteTaskCommand(taskToDelete.Id), CancellationToken.None);

        await UnitOfWork.Tasks.Received(1).DeletePermanent(taskToDelete.Id);
    }

    [Fact]
    public async Task Delete_ShouldCallCompleteAsync_WhenValidId()
    {
        var taskToDelete = AutoFixtures.ProjectTask;
        UnitOfWork.Tasks.GetAsync(Arg.Any<int>()).Returns(taskToDelete);

        await Handler.Handle(new DeleteTaskCommand(taskToDelete.Id), CancellationToken.None);

        await UnitOfWork.Received(1).CompleteAsync();
    }

    [Fact]
    public async Task Delete_ShouldReturnFailure_WhenInvalidId()
    {
        UnitOfWork.Tasks.GetAsync(Arg.Any<int>()).ReturnsNull();

        var result = await Handler.Handle(new DeleteTaskCommand(1), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Delete_ShouldNotCallDeletePermanent_WhenInvalidId()
    {
        UnitOfWork.Tasks.GetAsync(Arg.Any<int>()).ReturnsNull();

        await Handler.Handle(new DeleteTaskCommand(1), CancellationToken.None);

        await UnitOfWork.Tasks.Received(0).DeletePermanent(Arg.Any<int>());
    }

    [Fact]
    public async Task Delete_ShouldNotCallCompleteAsync_WhenInvalidId()
    {
        UnitOfWork.Tasks.GetAsync(Arg.Any<int>()).ReturnsNull();

        await Handler.Handle(new DeleteTaskCommand(1), CancellationToken.None);

        await UnitOfWork.Received(0).CompleteAsync();
    }

    [Fact]
    public async Task Delete_ShouldLogActivity_WhenValidId()
    {
        UnitOfWork.Tasks.GetAsync(Arg.Any<int>()).Returns(AutoFixtures.ProjectTask);

        await Handler.Handle(new DeleteTaskCommand(1), CancellationToken.None);

        Activity.Received(1).Log(Arg.Any<Action<ActivityOptions>>());
    }
}
