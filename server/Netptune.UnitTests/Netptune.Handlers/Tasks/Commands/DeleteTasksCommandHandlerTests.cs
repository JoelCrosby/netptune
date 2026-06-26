using FluentAssertions;

using Netptune.Core.Models.Activity;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Handlers.Tasks.Commands;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Handlers.Tasks.Commands;

public class DeleteTasksCommandHandlerTests
{
    private readonly DeleteTasksCommandHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();
    private readonly IActivityLogger Activity = Substitute.For<IActivityLogger>();

    public DeleteTasksCommandHandlerTests()
    {
        Handler = new(UnitOfWork, Identity, Activity);
    }

    [Fact]
    public async Task DeleteMany_ShouldReturnSuccess_WhenValidIds()
    {
        var ids = new[] { 1, 2, 3 };
        Identity.GetCurrentUserId().Returns("userId");
        UnitOfWork.Tasks.SoftDelete(Arg.Any<IEnumerable<int>>(), "userId", TestContext.Current.CancellationToken)
            .Returns(ids.ToList());

        var result = await Handler.Handle(new DeleteTasksCommand(ids), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteMany_ShouldSoftDeleteWithCurrentUser_WhenValidIds()
    {
        var ids = new[] { 1, 2, 3 };
        Identity.GetCurrentUserId().Returns("userId");
        UnitOfWork.Tasks.SoftDelete(Arg.Any<IEnumerable<int>>(), "userId", TestContext.Current.CancellationToken)
            .Returns(ids.ToList());

        await Handler.Handle(new DeleteTasksCommand(ids), TestContext.Current.CancellationToken);

        await UnitOfWork.Tasks.Received(1).SoftDelete(ids, "userId", TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task DeleteMany_ShouldNotLoadOrHardDeleteEntities_WhenValidIds()
    {
        var ids = new[] { 1, 2, 3 };
        Identity.GetCurrentUserId().Returns("userId");
        UnitOfWork.Tasks.SoftDelete(Arg.Any<IEnumerable<int>>(), "userId", TestContext.Current.CancellationToken)
            .Returns(ids.ToList());

        await Handler.Handle(new DeleteTasksCommand(ids), TestContext.Current.CancellationToken);

        await UnitOfWork.Tasks.Received(0).GetAllByIdAsync(Arg.Any<IEnumerable<int>>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
        await UnitOfWork.Tasks.Received(0).DeletePermanent(Arg.Any<IEnumerable<int>>(), Arg.Any<CancellationToken>());
        await UnitOfWork.Received(0).CompleteAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task DeleteMany_ShouldLogActivityForDeletedIds_WhenValidIds()
    {
        var ids = new[] { 1, 2, 3 };
        Identity.GetCurrentUserId().Returns("userId");
        UnitOfWork.Tasks.SoftDelete(Arg.Any<IEnumerable<int>>(), "userId", TestContext.Current.CancellationToken)
            .Returns(ids.ToList());

        await Handler.Handle(new DeleteTasksCommand(ids), TestContext.Current.CancellationToken);

        Activity.Received(1).LogMany(Arg.Any<Action<ActivityMultipleOptions>>());
    }
}
