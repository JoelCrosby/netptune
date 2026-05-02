using FluentAssertions;

using Netptune.Core.Entities;
using Netptune.Core.Models.Activity;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Services.Tasks.Commands;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Services.Tasks.Commands;

public class DeleteTasksCommandHandlerTests
{
    private readonly DeleteTasksCommandHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IActivityLogger Activity = Substitute.For<IActivityLogger>();

    public DeleteTasksCommandHandlerTests()
    {
        Handler = new(UnitOfWork, Activity);
    }

    [Fact]
    public async Task DeleteMany_ShouldReturnSuccess_WhenValidId()
    {
        var ids = new[] { 1, 2, 3 };
        UnitOfWork.Tasks.GetAllByIdAsync(Arg.Any<int[]>(), cancellationToken: TestContext.Current.CancellationToken).Returns(new List<ProjectTask>
        {
            new () { Id = 1 },
            new () { Id = 2 },
            new () { Id = 3 },
        });

        var result = await Handler.Handle(new DeleteTasksCommand(ids), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteMany_ShouldCallDeletePermanent_WhenValidId()
    {
        var ids = new[] { 1, 2, 3 };
        UnitOfWork.Tasks.GetAllByIdAsync(Arg.Any<int[]>(), cancellationToken: TestContext.Current.CancellationToken).Returns(new List<ProjectTask>
        {
            new () { Id = 1 },
            new () { Id = 2 },
            new () { Id = 3 },
        });

        await Handler.Handle(new DeleteTasksCommand(ids), TestContext.Current.CancellationToken);

        await UnitOfWork.Tasks.Received(1).DeletePermanent(Arg.Any<IEnumerable<int>>(), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task DeleteMany_ShouldCallCompleteAsync_WhenValidId()
    {
        var ids = new[] { 1, 2, 3 };
        UnitOfWork.Tasks.GetAllByIdAsync(Arg.Any<int[]>(), cancellationToken: TestContext.Current.CancellationToken).Returns(new List<ProjectTask>
        {
            new () { Id = 1 },
            new () { Id = 2 },
            new () { Id = 3 },
        });

        await Handler.Handle(new DeleteTasksCommand(ids), TestContext.Current.CancellationToken);

        await UnitOfWork.Received(1).CompleteAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task DeleteMany_ShouldLogActivity_WhenValidId()
    {
        var ids = new[] { 1, 2, 3 };
        UnitOfWork.Tasks.GetAllByIdAsync(Arg.Any<int[]>(), cancellationToken: TestContext.Current.CancellationToken).Returns(new List<ProjectTask>
        {
            new () { Id = 1 },
            new () { Id = 2 },
            new () { Id = 3 },
        });

        await Handler.Handle(new DeleteTasksCommand(ids), TestContext.Current.CancellationToken);

        Activity.Received(1).LogMany(Arg.Any<Action<ActivityMultipleOptions>>());
    }
}
