using AutoFixture;

using FluentAssertions;

using Netptune.Core.Entities;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Workspace;
using Netptune.Services.Workspaces.Commands;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Services.Workspaces.Commands;

public class CreateWorkspaceForNewUserCommandHandlerTests
{
    private readonly Fixture Fixture = new();
    private readonly CreateWorkspaceForNewUserCommandHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();

    public CreateWorkspaceForNewUserCommandHandlerTests()
    {
        Handler = new(UnitOfWork);
    }

    [Fact]
    public async Task CreateNewUserWorkspace_ShouldReturnCorrectly_WhenInputValid()
    {
        var request = Fixture.Build<AddWorkspaceRequest>().Create();
        var user = AutoFixtures.AppUser;

        UnitOfWork.InvokeTransaction<ClientResponse<WorkspaceViewModel>>();
        UnitOfWork.Workspaces.AddAsync(Arg.Any<Workspace>(), TestContext.Current.CancellationToken).Returns(x => x.Arg<Workspace>());
        UnitOfWork.Projects.GenerateProjectKey(Arg.Any<string>(), Arg.Any<int>(), TestContext.Current.CancellationToken).Returns("key");

        var result = await Handler.Handle(new CreateWorkspaceForNewUserCommand(request, user), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }
}
