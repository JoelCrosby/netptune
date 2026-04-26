using AutoFixture;

using FluentAssertions;

using Netptune.Core.Cache;
using Netptune.Core.Entities;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Workspace;
using Netptune.Services.Workspaces.Commands;
using Netptune.Services.Workspaces.Queries;

using NSubstitute;
using NSubstitute.ReturnsExtensions;

using Xunit;

namespace Netptune.UnitTests.Netptune.Services;

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
        UnitOfWork.Workspaces.AddAsync(Arg.Any<Workspace>()).Returns(x => x.Arg<Workspace>());
        UnitOfWork.Projects.GenerateProjectKey(Arg.Any<string>(), Arg.Any<int>()).Returns("key");

        var result = await Handler.Handle(new CreateWorkspaceCommand(request), CancellationToken.None);

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
        UnitOfWork.Workspaces.AddAsync(Arg.Any<Workspace>()).Returns(x => x.Arg<Workspace>());
        UnitOfWork.Projects.GenerateProjectKey(Arg.Any<string>(), Arg.Any<int>()).Returns("key");

        await Handler.Handle(new CreateWorkspaceCommand(request), CancellationToken.None);

        await UnitOfWork.Received(2).CompleteAsync();
    }
}

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
        UnitOfWork.Workspaces.AddAsync(Arg.Any<Workspace>()).Returns(x => x.Arg<Workspace>());
        UnitOfWork.Projects.GenerateProjectKey(Arg.Any<string>(), Arg.Any<int>()).Returns("key");

        var result = await Handler.Handle(new CreateWorkspaceForNewUserCommand(request, user), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }
}

public class DeleteWorkspaceCommandHandlerTests
{
    private readonly DeleteWorkspaceCommandHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();
    private readonly IWorkspaceUserCache Cache = Substitute.For<IWorkspaceUserCache>();
    private readonly IActivityLogger Activity = Substitute.For<IActivityLogger>();

    public DeleteWorkspaceCommandHandlerTests()
    {
        Handler = new(UnitOfWork, Identity, Cache, Activity);
    }

    [Fact]
    public async Task Delete_WithSlug_ShouldReturnSuccess_WhenValidId()
    {
        UnitOfWork.Workspaces.GetBySlug("workspace").Returns(AutoFixtures.Workspace);

        var result = await Handler.Handle(new DeleteWorkspaceCommand("workspace"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Delete_ShouldCallCompleteAsync_WhenValidId()
    {
        UnitOfWork.Workspaces.GetBySlug("workspace").Returns(AutoFixtures.Workspace);

        await Handler.Handle(new DeleteWorkspaceCommand("workspace"), CancellationToken.None);

        await UnitOfWork.Received(1).CompleteAsync();
    }

    [Fact]
    public async Task Delete_ShouldReturnFailure_WhenInvalidId()
    {
        UnitOfWork.Workspaces.GetBySlug("workspace").ReturnsNull();

        var result = await Handler.Handle(new DeleteWorkspaceCommand("workspace"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Delete_ShouldNotCallDeletePermanent_WhenInvalidId()
    {
        UnitOfWork.Workspaces.GetBySlug("workspace").ReturnsNull();

        await Handler.Handle(new DeleteWorkspaceCommand("workspace"), CancellationToken.None);

        await UnitOfWork.Tasks.Received(0).DeletePermanent(Arg.Any<int>());
    }

    [Fact]
    public async Task Delete_ShouldNotCallCompleteAsync_WhenInvalidId()
    {
        UnitOfWork.Workspaces.GetBySlug("workspace").ReturnsNull();

        await Handler.Handle(new DeleteWorkspaceCommand("workspace"), CancellationToken.None);

        await UnitOfWork.Received(0).CompleteAsync();
    }
}

public class DeleteWorkspacePermanentCommandHandlerTests
{
    private readonly DeleteWorkspacePermanentCommandHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();
    private readonly IWorkspaceUserCache Cache = Substitute.For<IWorkspaceUserCache>();

    public DeleteWorkspacePermanentCommandHandlerTests()
    {
        Handler = new(UnitOfWork, Identity, Cache);
    }

    [Fact]
    public async Task DeletePermanent_ShouldReturnSuccess_WhenValidId()
    {
        UnitOfWork.InvokeTransaction();
        UnitOfWork.Workspaces.GetBySlug("workspace").Returns(AutoFixtures.Workspace);

        var result = await Handler.Handle(new DeleteWorkspacePermanentCommand("workspace"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task DeletePermanent_ShouldNotCallDeletePermanent_WhenInvalidId()
    {
        UnitOfWork.InvokeTransaction();
        UnitOfWork.Workspaces.GetBySlug(Arg.Any<string>()).ReturnsNull();

        await Handler.Handle(new DeleteWorkspacePermanentCommand("workspace"), CancellationToken.None);

        await UnitOfWork.Tasks.Received(0).DeletePermanent(Arg.Any<int>());
    }

    [Fact]
    public async Task DeletePermanent_ShouldNotCallCompleteAsync_WhenInvalidId()
    {
        UnitOfWork.InvokeTransaction();
        UnitOfWork.Workspaces.GetBySlug(Arg.Any<string>()).ReturnsNull();

        await Handler.Handle(new DeleteWorkspacePermanentCommand("workspace"), CancellationToken.None);

        await UnitOfWork.Received(0).CompleteAsync();
    }
}

public class GetWorkspaceQueryHandlerTests
{
    private readonly GetWorkspaceQueryHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();

    public GetWorkspaceQueryHandlerTests()
    {
        Handler = new(UnitOfWork);
    }

    [Fact]
    public async Task GetWorkspace_ShouldReturnCorrectly_WhenInputValid()
    {
        var workspace = AutoFixtures.Workspace;
        UnitOfWork.Workspaces.GetBySlug("slug").Returns(workspace);

        var result = await Handler.Handle(new GetWorkspaceQuery("slug"), CancellationToken.None);

        result.Should().BeEquivalentTo(workspace);
    }
}

public class GetUserWorkspacesQueryHandlerTests
{
    private readonly GetUserWorkspacesQueryHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();

    public GetUserWorkspacesQueryHandlerTests()
    {
        Handler = new(UnitOfWork, Identity);
    }

    [Fact]
    public async Task GetUserWorkspaces_ShouldReturnCorrectly_WhenInputValid()
    {
        var workspaces = new List<Workspace> { AutoFixtures.Workspace };
        Identity.GetCurrentUserId().Returns("userId");
        UnitOfWork.Workspaces.GetUserWorkspaces("userId").Returns(workspaces);

        var result = await Handler.Handle(new GetUserWorkspacesQuery(), CancellationToken.None);

        result.Should().BeEquivalentTo(workspaces);
    }
}

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
        UnitOfWork.Workspaces.GetAllAsync().Returns(workspaces);

        var result = await Handler.Handle(new GetAllWorkspacesQuery(), CancellationToken.None);

        result.Should().BeEquivalentTo(workspaces);
    }
}

public class UpdateWorkspaceCommandHandlerTests
{
    private readonly Fixture Fixture = new();
    private readonly UpdateWorkspaceCommandHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();
    private readonly IActivityLogger Activity = Substitute.For<IActivityLogger>();

    public UpdateWorkspaceCommandHandlerTests()
    {
        Handler = new(UnitOfWork, Identity, Activity);
    }

    [Fact]
    public async Task Update_ShouldReturnCorrectly_WhenInputValid()
    {
        var request = Fixture.Build<UpdateWorkspaceRequest>().Create();
        var workspace = AutoFixtures.Workspace;

        Identity.GetWorkspaceKey().Returns("key");
        Identity.GetCurrentUserId().Returns(AutoFixtures.AppUser.Id);
        UnitOfWork.Workspaces.GetBySlug(Arg.Any<string>()).Returns(workspace);

        var result = await Handler.Handle(new UpdateWorkspaceCommand(request), CancellationToken.None);

        result.Should().NotBeNull();
        result.Payload.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Payload!.Name.Should().Be(request.Name);
        result.Payload.Description.Should().Be(request.Description);
        result.Payload.Slug.Should().NotBeNull();
    }

    [Fact]
    public async Task Update_ShouldCallCompleteAsync_WhenInputValid()
    {
        var request = Fixture.Build<UpdateWorkspaceRequest>().Create();

        Identity.GetWorkspaceKey().Returns("key");
        Identity.GetCurrentUserId().Returns(AutoFixtures.AppUser.Id);
        UnitOfWork.Workspaces.GetBySlug(Arg.Any<string>()).Returns(AutoFixtures.Workspace);

        await Handler.Handle(new UpdateWorkspaceCommand(request), CancellationToken.None);

        await UnitOfWork.Received(1).CompleteAsync();
    }

    [Fact]
    public async Task Update_ShouldReturnFailure_WhenWorkspaceNotFound()
    {
        var request = Fixture.Build<UpdateWorkspaceRequest>().Create();

        Identity.GetWorkspaceKey().Returns("key");
        Identity.GetCurrentUserId().Returns(AutoFixtures.AppUser.Id);
        UnitOfWork.Workspaces.GetBySlug(Arg.Any<string>()).ReturnsNull();

        var result = await Handler.Handle(new UpdateWorkspaceCommand(request), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }
}

public class IsWorkspaceSlugUniqueQueryHandlerTests
{
    private readonly IsWorkspaceSlugUniqueQueryHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();

    public IsWorkspaceSlugUniqueQueryHandlerTests()
    {
        Handler = new(UnitOfWork);
    }

    [Fact]
    public async Task IsSlugUnique_ShouldReturnCorrectly_WhenUnique()
    {
        UnitOfWork.Workspaces.Exists(Arg.Any<string>()).Returns(false);

        var result = await Handler.Handle(new IsWorkspaceSlugUniqueQuery("slug"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Payload?.IsUnique.Should().BeTrue();
    }

    [Fact]
    public async Task IsSlugUnique_ShouldReturnFailure_WhenNotUnique()
    {
        UnitOfWork.Workspaces.Exists(Arg.Any<string>()).Returns(true);

        var result = await Handler.Handle(new IsWorkspaceSlugUniqueQuery("slug"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Payload?.IsUnique.Should().BeFalse();
    }
}
