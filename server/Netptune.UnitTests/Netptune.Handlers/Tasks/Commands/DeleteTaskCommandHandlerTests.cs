using FluentAssertions;

using Netptune.Core.Models.Activity;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Handlers.Tasks.Commands;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Handlers.Tasks.Commands;

public class DeleteTaskCommandHandlerTests
{
    private readonly DeleteTaskCommandHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();
    private readonly IActivityLogger Activity = Substitute.For<IActivityLogger>();

    public DeleteTaskCommandHandlerTests()
    {
        Handler = new(UnitOfWork, Identity, Activity);
    }

    [Fact]
    public async Task Delete_ShouldReturnSuccess_WhenValidId()
    {
        Identity.GetCurrentUserId().Returns("userId");
        UnitOfWork.Tasks.SoftDelete(1, "userId", TestContext.Current.CancellationToken).Returns(1);

        var result = await Handler.Handle(new DeleteTaskCommand(1), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Delete_ShouldSoftDeleteWithCurrentUser_WhenValidId()
    {
        Identity.GetCurrentUserId().Returns("userId");
        UnitOfWork.Tasks.SoftDelete(1, "userId", TestContext.Current.CancellationToken).Returns(1);

        await Handler.Handle(new DeleteTaskCommand(1), TestContext.Current.CancellationToken);

        await UnitOfWork.Tasks.Received(1).SoftDelete(1, "userId", TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Delete_ShouldNotLoadOrHardDeleteEntity_WhenValidId()
    {
        Identity.GetCurrentUserId().Returns("userId");
        UnitOfWork.Tasks.SoftDelete(1, "userId", TestContext.Current.CancellationToken).Returns(1);

        await Handler.Handle(new DeleteTaskCommand(1), TestContext.Current.CancellationToken);

        await UnitOfWork.Tasks.Received(0).GetAsync(Arg.Any<int>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
        await UnitOfWork.Tasks.Received(0).DeletePermanent(Arg.Any<int>(), Arg.Any<CancellationToken>());
        await UnitOfWork.Received(0).CompleteAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Delete_ShouldLogActivity_WhenValidId()
    {
        Identity.GetCurrentUserId().Returns("userId");
        UnitOfWork.Tasks.SoftDelete(1, "userId", TestContext.Current.CancellationToken).Returns(1);

        await Handler.Handle(new DeleteTaskCommand(1), TestContext.Current.CancellationToken);

        Activity.Received(1).Log(Arg.Any<Action<ActivityOptions>>());
    }

    [Fact]
    public async Task Delete_ShouldReturnFailure_WhenInvalidId()
    {
        Identity.GetCurrentUserId().Returns("userId");
        UnitOfWork.Tasks.SoftDelete(Arg.Any<int>(), "userId", TestContext.Current.CancellationToken).Returns(0);

        var result = await Handler.Handle(new DeleteTaskCommand(1), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Delete_ShouldNotLogActivity_WhenInvalidId()
    {
        Identity.GetCurrentUserId().Returns("userId");
        UnitOfWork.Tasks.SoftDelete(Arg.Any<int>(), "userId", TestContext.Current.CancellationToken).Returns(0);

        await Handler.Handle(new DeleteTaskCommand(1), TestContext.Current.CancellationToken);

        Activity.Received(0).Log(Arg.Any<Action<ActivityOptions>>());
    }
}
