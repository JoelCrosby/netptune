using FluentAssertions;

using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Services.Projects.Commands;

using NSubstitute;
using NSubstitute.ReturnsExtensions;

using Xunit;

namespace Netptune.UnitTests.Netptune.Services.Projects.Commands;

public class DeleteProjectCommandHandlerTests
{
    private readonly DeleteProjectCommandHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();
    private readonly IActivityLogger Activity = Substitute.For<IActivityLogger>();

    public DeleteProjectCommandHandlerTests()
    {
        Handler = new(UnitOfWork, Identity, Activity);
    }

    [Fact]
    public async Task Delete_ShouldReturnSuccess_WhenValidId()
    {
        UnitOfWork.Projects.GetAsync(1, cancellationToken: TestContext.Current.CancellationToken).Returns(AutoFixtures.Project);

        var result = await Handler.Handle(new DeleteProjectCommand(1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Delete_ShouldCallCompleteAsync_WhenValidId()
    {
        UnitOfWork.Projects.GetAsync(1, cancellationToken: TestContext.Current.CancellationToken).Returns(AutoFixtures.Project);

        await Handler.Handle(new DeleteProjectCommand(1), CancellationToken.None);

        await UnitOfWork.Received(1).CompleteAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Delete_ShouldReturnFailure_WhenInvalidId()
    {
        UnitOfWork.Projects.GetAsync(1, cancellationToken: TestContext.Current.CancellationToken).ReturnsNull();

        var result = await Handler.Handle(new DeleteProjectCommand(1), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Delete_ShouldNotCallDeletePermanent_WhenInvalidId()
    {
        UnitOfWork.Projects.GetAsync(1, cancellationToken: TestContext.Current.CancellationToken).ReturnsNull();

        await Handler.Handle(new DeleteProjectCommand(1), CancellationToken.None);

        await UnitOfWork.Tasks.Received(0).DeletePermanent(Arg.Any<int>(), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Delete_ShouldNotCallCompleteAsync_WhenInvalidId()
    {
        UnitOfWork.Projects.GetAsync(1, cancellationToken: TestContext.Current.CancellationToken).ReturnsNull();

        await Handler.Handle(new DeleteProjectCommand(1), CancellationToken.None);

        await UnitOfWork.Received(0).CompleteAsync(TestContext.Current.CancellationToken);
    }
}
