using AutoFixture;

using FluentAssertions;

using Netptune.Core.Entities;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Projects;
using Netptune.Services.Projects.Commands;

using NSubstitute;
using NSubstitute.ReturnsExtensions;

using Xunit;

namespace Netptune.UnitTests.Netptune.Services.Projects.Commands;

public class CreateProjectCommandHandlerTests
{
    private readonly Fixture Fixture = new();
    private readonly CreateProjectCommandHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();
    private readonly IActivityLogger Activity = Substitute.For<IActivityLogger>();

    public CreateProjectCommandHandlerTests()
    {
        Handler = new(UnitOfWork, Identity, Activity);
    }

    [Fact]
    public async Task Create_ShouldReturnCorrectly_WhenInputValid()
    {
        var request = Fixture.Build<AddProjectRequest>().Create();
        var viewModel = Fixture.Build<ProjectViewModel>()
            .With(x => x.Name, request.Name)
            .With(x => x.Description, request.Description)
            .Create();
        var workspace = AutoFixtures.Workspace;

        Identity.GetWorkspaceKey().Returns("key");
        Identity.GetCurrentUser().Returns(AutoFixtures.AppUser);

        UnitOfWork.InvokeTransaction<ClientResponse<ProjectViewModel>>();
        UnitOfWork.Workspaces.GetBySlug(Arg.Any<string>()).Returns(workspace);
        UnitOfWork.Projects.AddAsync(Arg.Any<Project>()).Returns(x => x.Arg<Project>());
        UnitOfWork.Projects.GenerateProjectKey(Arg.Any<string>(), Arg.Any<int>()).Returns("key");
        UnitOfWork.Projects.GetProjectViewModel(Arg.Any<int>()).Returns(viewModel);

        var result = await Handler.Handle(new CreateProjectCommand(request), CancellationToken.None);

        result.Should().NotBeNull();
        result.Payload.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Payload!.Name.Should().Be(request.Name);
        result.Payload.Description.Should().Be(request.Description);
        result.Payload.Key.Should().NotBeNull();
    }

    [Fact]
    public async Task Create_ShouldCallCompleteAsync_WhenInputValid()
    {
        var request = Fixture.Build<AddProjectRequest>().Create();
        var viewModel = Fixture.Build<ProjectViewModel>()
            .With(x => x.Name, request.Name)
            .With(x => x.Description, request.Description)
            .Create();

        Identity.GetWorkspaceKey().Returns("key");
        Identity.GetCurrentUser().Returns(AutoFixtures.AppUser);

        UnitOfWork.InvokeTransaction<ClientResponse<ProjectViewModel>>();
        UnitOfWork.Workspaces.GetBySlug(Arg.Any<string>()).Returns(AutoFixtures.Workspace);
        UnitOfWork.Projects.AddAsync(Arg.Any<Project>()).Returns(x => x.Arg<Project>());
        UnitOfWork.Projects.GenerateProjectKey(Arg.Any<string>(), Arg.Any<int>()).Returns("key");
        UnitOfWork.Projects.GetProjectViewModel(Arg.Any<int>()).Returns(viewModel);

        await Handler.Handle(new CreateProjectCommand(request), CancellationToken.None);

        await UnitOfWork.Received(1).CompleteAsync();
    }

    [Fact]
    public async Task Create_ShouldReturnFailure_WhenWorkspaceNotFound()
    {
        var request = Fixture.Build<AddProjectRequest>().Create();

        Identity.GetWorkspaceKey().Returns("key");
        Identity.GetCurrentUser().Returns(AutoFixtures.AppUser);

        UnitOfWork.InvokeTransaction<ClientResponse<ProjectViewModel>>();
        UnitOfWork.Workspaces.GetBySlug(Arg.Any<string>()).ReturnsNull();

        var result = await Handler.Handle(new CreateProjectCommand(request), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }
}
