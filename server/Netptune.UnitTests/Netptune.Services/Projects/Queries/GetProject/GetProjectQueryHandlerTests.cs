using FluentAssertions;

using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Services.Projects.Queries;

using NSubstitute;
using NSubstitute.ReturnsExtensions;

using Xunit;

namespace Netptune.UnitTests.Netptune.Services.Projects.Queries;

public class GetProjectQueryHandlerTests
{
    private readonly GetProjectQueryHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();

    public GetProjectQueryHandlerTests()
    {
        Handler = new(UnitOfWork, Identity);
    }

    [Fact]
    public async Task GetProject_ShouldReturnCorrectly_WhenInputValid()
    {
        var viewModel = AutoFixtures.ProjectViewModel;

        Identity.GetWorkspaceKey().Returns("key");
        UnitOfWork.Workspaces.GetIdBySlug("key").Returns(1);
        UnitOfWork.Projects.GetProjectViewModel("key", 1).Returns(viewModel);

        var result = await Handler.Handle(new GetProjectQuery("key"), CancellationToken.None);

        result.Should().BeEquivalentTo(viewModel);
    }

    [Fact]
    public async Task GetProject_ShouldReturnNull_WhenProjectNotFound()
    {
        Identity.GetWorkspaceKey().Returns("key");
        UnitOfWork.Workspaces.GetIdBySlug("key").Returns(1);
        UnitOfWork.Projects.GetProjectViewModel("key", 1).ReturnsNull();

        var result = await Handler.Handle(new GetProjectQuery("key"), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetProject_ShouldReturnNull_WhenWorkspaceNotFound()
    {
        Identity.GetWorkspaceKey().Returns("key");
        UnitOfWork.Workspaces.GetIdBySlug("key").ReturnsNull();

        var result = await Handler.Handle(new GetProjectQuery("key"), CancellationToken.None);

        result.Should().BeNull();
    }
}
