using FluentAssertions;

using Netptune.Core.Entities;
using Netptune.Core.UnitOfWork;
using Netptune.Services.Workspaces.Queries;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Services.Workspaces.Queries;

public class GetAllWorkspacesQueryHandlerTests
{
    private readonly GetAllWorkspacesQueryHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();

    public GetAllWorkspacesQueryHandlerTests()
    {
        Handler = new(UnitOfWork);
    }

    [Fact]
    public async Task GetAll_ShouldReturnCorrectly_WhenInputValid()
    {
        var workspaces = new List<Workspace> { AutoFixtures.Workspace };
        UnitOfWork.Workspaces.GetAllAsync(cancellationToken: TestContext.Current.CancellationToken).Returns(workspaces);

        var result = await Handler.Handle(new GetAllWorkspacesQuery(), TestContext.Current.CancellationToken);

        result.Should().BeEquivalentTo(workspaces);
    }
}
