using FluentAssertions;

using Netptune.Core.Cache;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Services.Workspaces.Commands.DeleteWorkspace;

using NSubstitute;
using NSubstitute.ReturnsExtensions;

using Xunit;

namespace Netptune.UnitTests.Netptune.Services.Workspaces.Commands;

public class DeleteWorkspaceCommandHandlerTests
{
    private readonly DeleteWorkspaceCommandHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();
    private readonly IWorkspaceUserCache Cache = Substitute.For<IWorkspaceUserCache>();
    private readonly IActivityLogger Activity = Substitute.For<IActivityLogger>();

    public DeleteWorkspaceCommandHandlerTests()
    {
        Handler = new(UnitOfWork, Identity, Cache, Activity);
    }

    [Fact]
    public async Task Delete_WithSlug_ShouldReturnSuccess_WhenValidId()
    {
        UnitOfWork.Workspaces.GetBySlug("workspace").Returns(AutoFixtures.Workspace);

        var result = await Handler.Handle(new DeleteWorkspaceCommand("workspace"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Delete_ShouldCallCompleteAsync_WhenValidId()
    {
        UnitOfWork.Workspaces.GetBySlug("workspace").Returns(AutoFixtures.Workspace);

        await Handler.Handle(new DeleteWorkspaceCommand("workspace"), CancellationToken.None);

        await UnitOfWork.Received(1).CompleteAsync();
    }

    [Fact]
    public async Task Delete_ShouldReturnFailure_WhenInvalidId()
    {
        UnitOfWork.Workspaces.GetBySlug("workspace").ReturnsNull();

        var result = await Handler.Handle(new DeleteWorkspaceCommand("workspace"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Delete_ShouldNotCallDeletePermanent_WhenInvalidId()
    {
        UnitOfWork.Workspaces.GetBySlug("workspace").ReturnsNull();

        await Handler.Handle(new DeleteWorkspaceCommand("workspace"), CancellationToken.None);

        await UnitOfWork.Tasks.Received(0).DeletePermanent(Arg.Any<int>());
    }

    [Fact]
    public async Task Delete_ShouldNotCallCompleteAsync_WhenInvalidId()
    {
        UnitOfWork.Workspaces.GetBySlug("workspace").ReturnsNull();

        await Handler.Handle(new DeleteWorkspaceCommand("workspace"), CancellationToken.None);

        await UnitOfWork.Received(0).CompleteAsync();
    }
}
