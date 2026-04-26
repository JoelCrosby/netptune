using FluentAssertions;

using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Projects;
using Netptune.Services.Projects.Queries.GetProjects;

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
        UnitOfWork.Projects.GetProjects("key").Returns(viewModels);

        var result = await Handler.Handle(new GetProjectsQuery(), CancellationToken.None);

        result.Should().BeEquivalentTo(viewModels);
    }
}
