using FluentAssertions;

using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Projects;
using Netptune.Services.Projects.Queries;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Services.Projects.Queries;

public class GetProjectsQueryHandlerTests
{
    private readonly GetProjectsQueryHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();

    public GetProjectsQueryHandlerTests()
    {
        Handler = new(UnitOfWork, Identity);
    }

    [Fact]
    public async Task GetProjects_ShouldReturnCorrectly_WhenInputValid()
    {
        var viewModels = new List<ProjectViewModel> { AutoFixtures.ProjectViewModel };

        Identity.GetWorkspaceKey().Returns("key");
        UnitOfWork.Projects.GetProjects("key", TestContext.Current.CancellationToken).Returns(viewModels);

        var result = await Handler.Handle(new GetProjectsQuery(), TestContext.Current.CancellationToken);

        result.Should().BeEquivalentTo(viewModels);
    }
}
