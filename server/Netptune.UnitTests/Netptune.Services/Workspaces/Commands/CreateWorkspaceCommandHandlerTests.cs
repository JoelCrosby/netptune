using AutoFixture;

using FluentAssertions;

using Netptune.Core.Entities;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Workspace;
using Netptune.Services.Workspaces.Commands;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Services.Workspaces.Commands;

public class CreateWorkspaceCommandHandlerTests
{
    private readonly Fixture Fixture = new();
    private readonly CreateWorkspaceCommandHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();
    private readonly IActivityLogger Activity = Substitute.For<IActivityLogger>();

    public CreateWorkspaceCommandHandlerTests()
    {
        Handler = new(UnitOfWork, Identity, Activity);
    }

    [Fact]
    public async Task Create_ShouldReturnCorrectly_WhenInputValid()
    {
        var request = Fixture.Build<AddWorkspaceRequest>().Create();

        Identity.GetWorkspaceKey().Returns("key");
        Identity.GetCurrentUser().Returns(AutoFixtures.AppUser);
        UnitOfWork.InvokeTransaction<ClientResponse<WorkspaceViewModel>>();
        UnitOfWork.Workspaces.AddAsync(Arg.Any<Workspace>(), TestContext.Current.CancellationToken).Returns(x => x.Arg<Workspace>());
        UnitOfWork.Projects.GenerateProjectKey(Arg.Any<string>(), Arg.Any<int>(), TestContext.Current.CancellationToken).Returns("key");

        var result = await Handler.Handle(new CreateWorkspaceCommand(request), TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result.Payload.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Payload!.Name.Should().Be(request.Name);
        result.Payload.Description.Should().Be(request.Description);
        result.Payload.Slug.Should().NotBeNull();
    }

    [Fact]
    public async Task Create_CallCompleteAsync_WhenInputValid()
    {
        var request = Fixture.Build<AddWorkspaceRequest>().Create();

        Identity.GetWorkspaceKey().Returns("key");
        Identity.GetCurrentUser().Returns(AutoFixtures.AppUser);
        UnitOfWork.InvokeTransaction<ClientResponse<WorkspaceViewModel>>();
        UnitOfWork.Workspaces.AddAsync(Arg.Any<Workspace>(), TestContext.Current.CancellationToken).Returns(x => x.Arg<Workspace>());
        UnitOfWork.Projects.GenerateProjectKey(Arg.Any<string>(), Arg.Any<int>(), TestContext.Current.CancellationToken).Returns("key");

        await Handler.Handle(new CreateWorkspaceCommand(request), TestContext.Current.CancellationToken);

        await UnitOfWork.Received(2).CompleteAsync(TestContext.Current.CancellationToken);
    }
}
