using FluentAssertions;

using Netptune.Core.Cache;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Services.Workspaces.Commands.DeleteWorkspacePermanent;

using NSubstitute;
using NSubstitute.ReturnsExtensions;

using Xunit;

namespace Netptune.UnitTests.Netptune.Services.Workspaces.Commands;

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
        UnitOfWork.InvokeTransaction();
        UnitOfWork.Workspaces.GetBySlug("workspace").Returns(AutoFixtures.Workspace);

        var result = await Handler.Handle(new DeleteWorkspacePermanentCommand("workspace"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task DeletePermanent_ShouldNotCallDeletePermanent_WhenInvalidId()
    {
        UnitOfWork.InvokeTransaction();
        UnitOfWork.Workspaces.GetBySlug(Arg.Any<string>()).ReturnsNull();

        await Handler.Handle(new DeleteWorkspacePermanentCommand("workspace"), CancellationToken.None);

        await UnitOfWork.Tasks.Received(0).DeletePermanent(Arg.Any<int>());
    }

    [Fact]
    public async Task DeletePermanent_ShouldNotCallCompleteAsync_WhenInvalidId()
    {
        UnitOfWork.InvokeTransaction();
        UnitOfWork.Workspaces.GetBySlug(Arg.Any<string>()).ReturnsNull();

        await Handler.Handle(new DeleteWorkspacePermanentCommand("workspace"), CancellationToken.None);

        await UnitOfWork.Received(0).CompleteAsync();
    }
}
