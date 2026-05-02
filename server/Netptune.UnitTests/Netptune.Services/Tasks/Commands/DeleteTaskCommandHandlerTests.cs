using FluentAssertions;

using Netptune.Core.Models.Activity;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Services.Tasks.Commands;

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
        UnitOfWork.Tasks.GetAsync(1, cancellationToken: TestContext.Current.CancellationToken).Returns(AutoFixtures.ProjectTask);

        var result = await Handler.Handle(new DeleteTaskCommand(1), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Delete_ShouldCallDeletePermanent_WhenValidId()
    {
        var taskToDelete = AutoFixtures.ProjectTask;
        UnitOfWork.Tasks.GetAsync(Arg.Any<int>(), cancellationToken: TestContext.Current.CancellationToken).Returns(taskToDelete);

        await Handler.Handle(new DeleteTaskCommand(taskToDelete.Id), TestContext.Current.CancellationToken);

        await UnitOfWork.Tasks.Received(1).DeletePermanent(taskToDelete.Id, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Delete_ShouldCallCompleteAsync_WhenValidId()
    {
        var taskToDelete = AutoFixtures.ProjectTask;
        UnitOfWork.Tasks.GetAsync(Arg.Any<int>(), cancellationToken: TestContext.Current.CancellationToken).Returns(taskToDelete);

        await Handler.Handle(new DeleteTaskCommand(taskToDelete.Id), TestContext.Current.CancellationToken);

        await UnitOfWork.Received(1).CompleteAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Delete_ShouldReturnFailure_WhenInvalidId()
    {
        UnitOfWork.Tasks.GetAsync(Arg.Any<int>(), cancellationToken: TestContext.Current.CancellationToken).ReturnsNull();

        var result = await Handler.Handle(new DeleteTaskCommand(1), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Delete_ShouldNotCallDeletePermanent_WhenInvalidId()
    {
        UnitOfWork.Tasks.GetAsync(Arg.Any<int>(), cancellationToken: TestContext.Current.CancellationToken).ReturnsNull();

        await Handler.Handle(new DeleteTaskCommand(1), TestContext.Current.CancellationToken);

        await UnitOfWork.Tasks.Received(0).DeletePermanent(Arg.Any<int>(), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Delete_ShouldNotCallCompleteAsync_WhenInvalidId()
    {
        UnitOfWork.Tasks.GetAsync(Arg.Any<int>(), cancellationToken: TestContext.Current.CancellationToken).ReturnsNull();

        await Handler.Handle(new DeleteTaskCommand(1), TestContext.Current.CancellationToken);

        await UnitOfWork.Received(0).CompleteAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Delete_ShouldLogActivity_WhenValidId()
    {
        UnitOfWork.Tasks.GetAsync(Arg.Any<int>(), cancellationToken: TestContext.Current.CancellationToken).Returns(AutoFixtures.ProjectTask);

        await Handler.Handle(new DeleteTaskCommand(1), TestContext.Current.CancellationToken);

        Activity.Received(1).Log(Arg.Any<Action<ActivityOptions>>());
    }
}
