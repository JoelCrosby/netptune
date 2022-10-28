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
using NSubstitute.ReturnsExtensions;

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

        result.Should().NotBeNull();
        result.Payload.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Payload!.Name.Should().Be(request.Name);
        result.Payload.Description.Should().Be(request.Description);
        result.Payload.Slug.Should().NotBeNull();
    }

    [Fact]
    public async Task AddWorkspace_CallCompleteAsync_WhenInputValid()
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

        await Service.Create(request);

        await UnitOfWork.Received(2).CompleteAsync();
    }

    [Fact]
    public async Task Delete_WithSlug_ShouldReturnSuccess_WhenValidId()
    {
        var workspace = AutoFixtures.Workspace;

        UnitOfWork.Workspaces.GetAsync(workspace.Id).Returns(workspace);
        UnitOfWork.Workspaces.GetBySlug("workspace").Returns(workspace);

        var result = await Service.Delete("workspace");

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Delete_ShouldReturnSuccess_WhenValidId()
    {
        var workspace = AutoFixtures.Workspace;

        UnitOfWork.Workspaces.GetAsync(workspace.Id).Returns(workspace);
        UnitOfWork.Workspaces.GetBySlug("workspace").Returns(workspace);

        var result = await Service.Delete("workspace");

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Delete_ShouldCallCompleteAsync_WhenValidId()
    {
        var workspace = AutoFixtures.Workspace;

        UnitOfWork.Workspaces.GetBySlug("workspace").Returns(workspace);

        await Service.Delete("workspace");

        await UnitOfWork.Received(1).CompleteAsync();
    }

    [Fact]
    public async Task Delete_ShouldReturnFailure_WhenInvalidId()
    {
        UnitOfWork.Workspaces.GetBySlug("workspace").ReturnsNull();

        var result = await Service.Delete("workspace");

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Delete_ShouldNotCallDeletePermanent_WhenValidId()
    {
        UnitOfWork.Workspaces.GetBySlug("workspace").ReturnsNull();

        await Service.Delete("workspace");

        await UnitOfWork.Tasks.Received(0).DeletePermanent(Arg.Any<int>());
    }

    [Fact]
    public async Task Delete_ShouldNotCallCompleteAsync_WhenValidId()
    {
        UnitOfWork.Workspaces.GetBySlug("workspace").ReturnsNull();

        await Service.Delete("workspace");

        await UnitOfWork.Received(0).CompleteAsync();
    }

    [Fact]
    public async Task DeletePermanent_WithSlug_ShouldReturnSuccess_WhenValidId()
    {
        var workspace = AutoFixtures.Workspace;

        UnitOfWork.Transaction(Arg.Any<Func<Task>>())
            .Returns(x => x.Arg<Func<Task>>()
                .Invoke());

        UnitOfWork.Workspaces.GetBySlug("workspace").Returns(workspace);

        var result = await Service.DeletePermanent("workspace");

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task DeletePermanent_ShouldNotCallDeletePermanent_WhenValidId()
    {
        UnitOfWork.Transaction(Arg.Any<Func<Task>>())
            .Returns(x => x.Arg<Func<Task>>()
                .Invoke());

        UnitOfWork.Workspaces.GetBySlug(Arg.Any<string>()).ReturnsNull();

        await Service.DeletePermanent("workspace");

        await UnitOfWork.Tasks.Received(0).DeletePermanent(Arg.Any<int>());
    }

    [Fact]
    public async Task DeletePermanent_ShouldNotCallCompleteAsync_WhenValidId()
    {
        UnitOfWork.Transaction(Arg.Any<Func<Task>>())
            .Returns(x => x.Arg<Func<Task>>()
                .Invoke());

        UnitOfWork.Workspaces.GetBySlug(Arg.Any<string>()).ReturnsNull();

        await Service.DeletePermanent("workspace");

        await UnitOfWork.Received(0).CompleteAsync();
    }

    [Fact]
    public async Task GetWorkspace_ShouldReturnCorrectly_WhenInputValid()
    {
        var workspace = AutoFixtures.Workspace;

        UnitOfWork.Workspaces.GetBySlug("slug").Returns(workspace);

        var result = await Service.GetWorkspace("slug");

        result.Should().BeEquivalentTo(workspace);
    }

    [Fact]
    public async Task GetUserWorkspaces_ShouldReturnCorrectly_WhenInputValid()
    {
        var workspaces = new List<Workspace>{ AutoFixtures.Workspace };

        Identity.GetCurrentUserId().Returns("userId");
        UnitOfWork.Workspaces.GetUserWorkspaces("userId").Returns(workspaces);

        var result = await Service.GetUserWorkspaces();

        result.Should().BeEquivalentTo(workspaces);
    }

    [Fact]
    public async Task GetAll_ShouldReturnCorrectly_WhenInputValid()
    {
        var workspaces = new List<Workspace>{ AutoFixtures.Workspace };

        UnitOfWork.Workspaces.GetAllAsync().Returns(workspaces);

        var result = await Service.GetAll();

        result.Should().BeEquivalentTo(workspaces);
    }

    [Fact]
    public async Task Update_ShouldReturnCorrectly_WhenInputValid()
    {
        var request = Fixture
            .Build<UpdateWorkspaceRequest>()
            .Create();

        var workspace = AutoFixtures.Workspace;

        Identity.GetWorkspaceKey().Returns("key");
        Identity.GetCurrentUserId().Returns(AutoFixtures.AppUser.Id);

        UnitOfWork.Workspaces.GetBySlug(Arg.Any<string>()).Returns(workspace);

        var result = await Service.Update(request);

        result.Should().NotBeNull();
        result.Payload.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Payload!.Name.Should().Be(request.Name);
        result.Payload.Description.Should().Be(request.Description);
        result.Payload.Slug.Should().NotBeNull();
    }

    [Fact]
    public async Task Update_CallCompleteAsync_WhenInputValid()
    {
        var request = Fixture
            .Build<UpdateWorkspaceRequest>()
            .Create();

        var workspace = AutoFixtures.Workspace;

        Identity.GetWorkspaceKey().Returns("key");
        Identity.GetCurrentUserId().Returns(AutoFixtures.AppUser.Id);

        UnitOfWork.Workspaces.GetBySlug(Arg.Any<string>()).Returns(workspace);

        await Service.Update(request);

        await UnitOfWork.Received(1).CompleteAsync();
    }

    [Fact]
    public async Task Update_ShouldReturnFailure_WhenWorkspaceNotFound()
    {
        var request = Fixture
            .Build<UpdateWorkspaceRequest>()
            .Create();

        Identity.GetWorkspaceKey().Returns("key");
        Identity.GetCurrentUserId().Returns(AutoFixtures.AppUser.Id);

        UnitOfWork.Workspaces.ReturnsNull();

        var result = await Service.Update(request);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task IsSlugUnique_ShouldReturnCorrectly_WhenUnique()
    {
        UnitOfWork.Workspaces.Exists(Arg.Any<string>()).Returns(false);

        var result = await Service.IsSlugUnique("slug");

        result.IsSuccess.Should().BeTrue();
        result.Payload?.IsUnique.Should().BeTrue();
    }

    [Fact]
    public async Task IsSlugUnique_ShouldReturnFailure_WhenNotUnique()
    {
        UnitOfWork.Workspaces.Exists(Arg.Any<string>()).Returns(true);

        var result = await Service.IsSlugUnique("slug");

        result.IsSuccess.Should().BeTrue();
        result.Payload?.IsUnique.Should().BeFalse();
    }
}
