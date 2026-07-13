using FluentAssertions;

using Netptune.Core.Cache;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Handlers.Workspaces.Commands;

using NSubstitute;
using NSubstitute.ReturnsExtensions;

using Xunit;

namespace Netptune.UnitTests.Netptune.Handlers.Workspaces.Commands;

public class DeleteWorkspacePermanentCommandHandlerTests
{
    private readonly DeleteWorkspacePermanentCommandHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();
    private readonly IWorkspaceUserCache Cache = Substitute.For<IWorkspaceUserCache>();

    public DeleteWorkspacePermanentCommandHandlerTests()
    {
        Handler = new(UnitOfWork, Identity, Cache);
    }

    [Fact]
    public async Task DeletePermanent_ShouldReturnSuccess_WhenValidId()
    {
        var workspace = AutoFixtures.Workspace;

        UnitOfWork.InvokeTransaction();
        UnitOfWork.Workspaces.GetBySlug("workspace", cancellationToken: TestContext.Current.CancellationToken).Returns(workspace);

        var result = await Handler.Handle(new DeleteWorkspacePermanentCommand("workspace"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();

        await UnitOfWork.Workspaces
            .Received(1)
            .DeleteWorkspacePermanent(workspace.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeletePermanent_ShouldNotDeleteAnything_WhenInvalidId()
    {
        UnitOfWork.InvokeTransaction();
        UnitOfWork.Workspaces.GetBySlug(Arg.Any<string>(), cancellationToken: TestContext.Current.CancellationToken).ReturnsNull();

        await Handler.Handle(new DeleteWorkspacePermanentCommand("workspace"), TestContext.Current.CancellationToken);

        await UnitOfWork.Workspaces
            .DidNotReceive()
            .DeleteWorkspacePermanent(Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeletePermanent_ShouldNotOpenATransaction_WhenInvalidId()
    {
        UnitOfWork.InvokeTransaction();
        UnitOfWork.Workspaces.GetBySlug(Arg.Any<string>(), cancellationToken: TestContext.Current.CancellationToken).ReturnsNull();

        await Handler.Handle(new DeleteWorkspacePermanentCommand("workspace"), TestContext.Current.CancellationToken);

        await UnitOfWork.DidNotReceive().Transaction(Arg.Any<Func<Task>>(), Arg.Any<bool>());
    }
}
