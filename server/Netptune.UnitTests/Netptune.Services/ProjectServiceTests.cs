using AutoFixture;

using FluentAssertions;

using Netptune.Core.Entities;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Projects;
using Netptune.Services;

using NSubstitute;
using NSubstitute.ReturnsExtensions;

using Xunit;

namespace Netptune.UnitTests.Netptune.Services;

public class ProjectServiceTests
{
    private readonly Fixture Fixture = new();

    private readonly ProjectService Service;

    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();

    public ProjectServiceTests()
    {
        Service = new(UnitOfWork, Identity);
    }

    [Fact]
    public async Task Create_ShouldReturnCorrectly_WhenInputValid()
    {
        var request = Fixture
            .Build<AddProjectRequest>()
            .Create();

        var viewModel = Fixture.Build<ProjectViewModel>()
            .With(x => x.Name, request.Name)
            .With(x => x.Description, request.Description)
            .Create();

        var workspace = AutoFixtures.Workspace;

        Identity.GetWorkspaceKey().Returns("key");
        Identity.GetCurrentUser().Returns(AutoFixtures.AppUser);

        UnitOfWork.Transaction(Arg.Any<Func<Task<ClientResponse<ProjectViewModel>>>>())
            .Returns(x => x.Arg<Func<Task<ClientResponse<ProjectViewModel>>>>()
                .Invoke());

        UnitOfWork.Workspaces.GetBySlug(Arg.Any<string>()).Returns(workspace);
        UnitOfWork.Projects.AddAsync(Arg.Any<Project>()).Returns(x => x.Arg<Project>());
        UnitOfWork.Projects.GenerateProjectKey(Arg.Any<string>(), Arg.Any<int>()).Returns("key");
        UnitOfWork.Projects.GetProjectViewModel(Arg.Any<int>()).Returns(viewModel);

        var result = await Service.Create(request);

        result.Should().NotBeNull();
        result.Payload.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Payload!.Name.Should().Be(request.Name);
        result.Payload.Description.Should().Be(request.Description);
        result.Payload.Key.Should().NotBeNull();
    }

    [Fact]
    public async Task Create_CallCompleteAsync_WhenInputValid()
    {
        var request = Fixture
            .Build<AddProjectRequest>()
            .Create();

        var viewModel = Fixture.Build<ProjectViewModel>()
            .With(x => x.Name, request.Name)
            .With(x => x.Description, request.Description)
            .Create();

        var workspace = AutoFixtures.Workspace;

        Identity.GetWorkspaceKey().Returns("key");
        Identity.GetCurrentUser().Returns(AutoFixtures.AppUser);

        UnitOfWork.Transaction(Arg.Any<Func<Task<ClientResponse<ProjectViewModel>>>>())
            .Returns(x => x.Arg<Func<Task<ClientResponse<ProjectViewModel>>>>()
                .Invoke());

        UnitOfWork.Workspaces.GetBySlug(Arg.Any<string>()).Returns(workspace);
        UnitOfWork.Projects.AddAsync(Arg.Any<Project>()).Returns(x => x.Arg<Project>());
        UnitOfWork.Projects.GenerateProjectKey(Arg.Any<string>(), Arg.Any<int>()).Returns("key");
        UnitOfWork.Projects.GetProjectViewModel(Arg.Any<int>()).Returns(viewModel);

        await Service.Create(request);

        await UnitOfWork.Received(1).CompleteAsync();
    }

    [Fact]
    public async Task Delete_ShouldReturnSuccess_WhenValidId()
    {
        var project = AutoFixtures.Project;

        UnitOfWork.Projects.GetAsync(1).Returns(project);

        var result = await Service.Delete(1);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Delete_ShouldCallCompleteAsync_WhenValidId()
    {
        var project = AutoFixtures.Project;

        UnitOfWork.Projects.GetAsync(1).Returns(project);

        await Service.Delete(1);

        await UnitOfWork.Received(1).CompleteAsync();
    }

    [Fact]
    public async Task Delete_ShouldReturnFailure_WhenInvalidId()
    {
        UnitOfWork.Projects.GetAsync(1).ReturnsNull();

        var result = await Service.Delete(1);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Delete_ShouldNotCallDeletePermanent_WhenValidId()
    {
        UnitOfWork.Projects.GetAsync(1).ReturnsNull();

        await Service.Delete(1);

        await UnitOfWork.Tasks.Received(0).DeletePermanent(Arg.Any<int>());
    }

    [Fact]
    public async Task Delete_ShouldNotCallCompleteAsync_WhenValidId()
    {
        UnitOfWork.Projects.GetAsync(1).ReturnsNull();

        await Service.Delete(1);

        await UnitOfWork.Received(0).CompleteAsync();
    }

    [Fact]
    public async Task GetProject_ShouldReturnCorrectly_WhenInputValid()
    {
        var viewModel = AutoFixtures.ProjectViewModel;

        Identity.GetWorkspaceKey().Returns("key");
        UnitOfWork.Workspaces.GetIdBySlug("key").Returns(1);
        UnitOfWork.Projects.GetProjectViewModel("key", 1).Returns(viewModel);

        var result = await Service.GetProject("key");

        result.Should().BeEquivalentTo(viewModel);
    }

    [Fact]
    public async Task GetProject_ShouldReturnNull_WhenProjectNotFound()
    {
        Identity.GetWorkspaceKey().Returns("key");
        UnitOfWork.Workspaces.GetIdBySlug("key").Returns(1);
        UnitOfWork.Projects.GetProjectViewModel("key", 1).ReturnsNull();

        var result = await Service.GetProject("key");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetProject_ShouldReturnNull_WhenWorkspaceNotFound()
    {
        var viewModel = AutoFixtures.ProjectViewModel;

        Identity.GetWorkspaceKey().Returns("key");
        UnitOfWork.Workspaces.GetIdBySlug("key").ReturnsNull();
        UnitOfWork.Projects.GetProjectViewModel("key", 1).Returns(viewModel);

        var result = await Service.GetProject("key");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetProjects_ShouldReturnCorrectly_WhenInputValid()
    {
        var viewModels = new List<ProjectViewModel> { AutoFixtures.ProjectViewModel };

        Identity.GetWorkspaceKey().Returns("key");
        UnitOfWork.Projects.GetProjects("key").Returns(viewModels);

        var result = await Service.GetProjects();

        result.Should().BeEquivalentTo(viewModels);
    }

    [Fact]
    public async Task Update_ShouldReturnCorrectly_WhenInputValid()
    {
        var request = Fixture
            .Build<UpdateProjectRequest>()
            .Create();

        var user = AutoFixtures.AppUser;
        var project = AutoFixtures.Project;

        Identity.GetCurrentUser().Returns(user);
        UnitOfWork.Projects.GetWithIncludes(Arg.Any<int>()).Returns(project);

        var result = await Service.Update(request);

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
        var request = Fixture
            .Build<UpdateProjectRequest>()
            .Create();

        var user = AutoFixtures.AppUser;
        var project = AutoFixtures.Project;

        Identity.GetCurrentUser().Returns(user);
        UnitOfWork.Projects.GetWithIncludes(Arg.Any<int>()).Returns(project);

        await Service.Update(request);

        await UnitOfWork.Received(1).CompleteAsync();
    }

    [Fact]
    public async Task Update_ShouldReturnFailure_WhenUserNotFound()
    {
        var request = Fixture
            .Build<UpdateProjectRequest>()
            .Create();

        var user = AutoFixtures.AppUser;

        Identity.GetCurrentUser().Returns(user);
        UnitOfWork.Projects.GetWithIncludes(Arg.Any<int>()).ReturnsNull();

        var result = await Service.Update(request);

        result.IsSuccess.Should().BeFalse();
    }
}
