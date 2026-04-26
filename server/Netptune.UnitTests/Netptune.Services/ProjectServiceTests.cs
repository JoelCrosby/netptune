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
using Netptune.Services.Projects.Queries;

using NSubstitute;
using NSubstitute.ReturnsExtensions;

using Xunit;

namespace Netptune.UnitTests.Netptune.Services;

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

public class DeleteProjectCommandHandlerTests
{
    private readonly DeleteProjectCommandHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();
    private readonly IActivityLogger Activity = Substitute.For<IActivityLogger>();

    public DeleteProjectCommandHandlerTests()
    {
        Handler = new(UnitOfWork, Identity, Activity);
    }

    [Fact]
    public async Task Delete_ShouldReturnSuccess_WhenValidId()
    {
        UnitOfWork.Projects.GetAsync(1).Returns(AutoFixtures.Project);

        var result = await Handler.Handle(new DeleteProjectCommand(1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Delete_ShouldCallCompleteAsync_WhenValidId()
    {
        UnitOfWork.Projects.GetAsync(1).Returns(AutoFixtures.Project);

        await Handler.Handle(new DeleteProjectCommand(1), CancellationToken.None);

        await UnitOfWork.Received(1).CompleteAsync();
    }

    [Fact]
    public async Task Delete_ShouldReturnFailure_WhenInvalidId()
    {
        UnitOfWork.Projects.GetAsync(1).ReturnsNull();

        var result = await Handler.Handle(new DeleteProjectCommand(1), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Delete_ShouldNotCallDeletePermanent_WhenInvalidId()
    {
        UnitOfWork.Projects.GetAsync(1).ReturnsNull();

        await Handler.Handle(new DeleteProjectCommand(1), CancellationToken.None);

        await UnitOfWork.Tasks.Received(0).DeletePermanent(Arg.Any<int>());
    }

    [Fact]
    public async Task Delete_ShouldNotCallCompleteAsync_WhenInvalidId()
    {
        UnitOfWork.Projects.GetAsync(1).ReturnsNull();

        await Handler.Handle(new DeleteProjectCommand(1), CancellationToken.None);

        await UnitOfWork.Received(0).CompleteAsync();
    }
}

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

public class UpdateProjectCommandHandlerTests
{
    private readonly Fixture Fixture = new();
    private readonly UpdateProjectCommandHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();
    private readonly IActivityLogger Activity = Substitute.For<IActivityLogger>();

    public UpdateProjectCommandHandlerTests()
    {
        Handler = new(UnitOfWork, Identity, Activity);
    }

    [Fact]
    public async Task Update_ShouldReturnCorrectly_WhenInputValid()
    {
        var request = Fixture.Build<UpdateProjectRequest>().Create();
        var user = AutoFixtures.AppUser;
        var project = AutoFixtures.Project;

        Identity.GetCurrentUser().Returns(user);
        UnitOfWork.Projects.GetWithIncludes(Arg.Any<int>()).Returns(project);

        var result = await Handler.Handle(new UpdateProjectCommand(request), CancellationToken.None);

        result.Should().NotBeNull();
        result.Payload.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Payload!.Name.Should().Be(request.Name);
        result.Payload!.Description.Should().Be(request.Description);
        result.Payload!.RepositoryUrl.Should().Be(request.RepositoryUrl);
    }

    [Fact]
    public async Task Update_ShouldCallCompleteAsync_WhenInputValid()
    {
        var request = Fixture.Build<UpdateProjectRequest>().Create();

        Identity.GetCurrentUser().Returns(AutoFixtures.AppUser);
        UnitOfWork.Projects.GetWithIncludes(Arg.Any<int>()).Returns(AutoFixtures.Project);

        await Handler.Handle(new UpdateProjectCommand(request), CancellationToken.None);

        await UnitOfWork.Received(1).CompleteAsync();
    }

    [Fact]
    public async Task Update_ShouldReturnFailure_WhenProjectNotFound()
    {
        var request = Fixture.Build<UpdateProjectRequest>().Create();

        Identity.GetCurrentUser().Returns(AutoFixtures.AppUser);
        UnitOfWork.Projects.GetWithIncludes(Arg.Any<int>()).ReturnsNull();

        var result = await Handler.Handle(new UpdateProjectCommand(request), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }
}
