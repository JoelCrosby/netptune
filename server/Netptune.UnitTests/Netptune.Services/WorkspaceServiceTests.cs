using AutoFixture;

using FluentAssertions;

using Netptune.Core.Cache;
using Netptune.Core.Entities;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Workspace;
using Netptune.Services;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Services;

public class WorkspaceServiceTests
{
    private readonly Fixture Fixture = new();

    private readonly WorkspaceService Service;

    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();
    private readonly IWorkspaceUserCache Cache = Substitute.For<IWorkspaceUserCache>();

    public WorkspaceServiceTests()
    {
        Service = new(UnitOfWork, Identity, Cache);
    }

    [Fact]
    public async Task AddWorkspace_ShouldReturnCorrectly_WhenInputValid()
    {
        var request = Fixture
            .Build<AddWorkspaceRequest>()
            .Create();

        Identity.GetWorkspaceKey().Returns("key");
        Identity.GetCurrentUser().Returns(AutoFixtures.AppUser);

        UnitOfWork.Transaction(Arg.Any<Func<Task<ClientResponse<WorkspaceViewModel>>>>())
            .Returns(x => x.Arg<Func<Task<ClientResponse<WorkspaceViewModel>>>>()
                .Invoke());

        UnitOfWork.Workspaces.AddAsync(Arg.Any<Workspace>()).Returns(x => x.Arg<Workspace>());
        UnitOfWork.Projects.GenerateProjectKey(Arg.Any<string>(), Arg.Any<int>()).Returns("key");

        var result = await Service.Create(request);

        result.IsSuccess.Should().BeTrue();
    }
}
