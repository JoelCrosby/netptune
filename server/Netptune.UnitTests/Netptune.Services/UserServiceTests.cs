using AutoFixture;

using FluentAssertions;

using Netptune.Core.Authorization;
using Netptune.Core.Cache;
using Netptune.Core.Entities;
using Netptune.Core.Messaging;
using Netptune.Core.Models;
using Netptune.Core.Models.Messaging;
using Netptune.Core.Relationships;
using Netptune.Core.Requests;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Users;
using Netptune.Services.Users.Commands;
using Netptune.Services.Users.Queries;

using NSubstitute;
using NSubstitute.ReturnsExtensions;

using Xunit;

namespace Netptune.UnitTests.Netptune.Services;

public class GetUserQueryHandlerTests
{
    private readonly GetUserQueryHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();
    private readonly IWorkspacePermissionCache WorkspacePermissionCache = Substitute.For<IWorkspacePermissionCache>();

    public GetUserQueryHandlerTests()
    {
        Handler = new(UnitOfWork, Identity, WorkspacePermissionCache);
    }

    [Fact]
    public async Task Get_ShouldReturnCorrectly_WhenInputValid()
    {
        var user = AutoFixtures.AppUser;
        const string workspaceKey = "workspaceKey";

        UnitOfWork.Users.GetAsync("userId", Arg.Any<bool>()).Returns(user);
        Identity.GetWorkspaceKey().Returns(workspaceKey);
        WorkspacePermissionCache.GetUserPermissions(user.Id, workspaceKey).Returns(new UserPermissions
        {
            Permissions = [],
            Role = WorkspaceRole.Owner,
            UserId = user.Id,
            WorkspaceKey = workspaceKey,
        });

        var result = await Handler.Handle(new GetUserQuery("userId"), CancellationToken.None);

        result.Should().BeEquivalentTo(new UserViewModel
        {
            Id = user.Id,
            Firstname = user.Firstname,
            Lastname = user.Lastname,
            PictureUrl = user.PictureUrl,
            DisplayName = user.DisplayName,
            Email = user.Email!,
            UserName = user.UserName!,
            LastLoginTime = user.LastLoginTime,
            RegistrationDate = user.RegistrationDate,
            Permissions = [],
        });
    }

    [Fact]
    public async Task Get_ShouldReturnNull_WhenUserNotFound()
    {
        UnitOfWork.Users.GetAsync("userId", Arg.Any<bool>()).ReturnsNull();

        var result = await Handler.Handle(new GetUserQuery("userId"), CancellationToken.None);

        result.Should().BeNull();
    }
}

public class GetUserByEmailQueryHandlerTests
{
    private readonly GetUserByEmailQueryHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();
    private readonly IWorkspacePermissionCache WorkspacePermissionCache = Substitute.For<IWorkspacePermissionCache>();

    public GetUserByEmailQueryHandlerTests()
    {
        Handler = new(UnitOfWork, Identity, WorkspacePermissionCache);
    }

    [Fact]
    public async Task GetByEmail_ShouldReturnCorrectly_WhenInputValid()
    {
        var user = AutoFixtures.AppUser;
        const string workspaceKey = "workspaceKey";

        UnitOfWork.Users.GetByEmail("email", Arg.Any<bool>()).Returns(user);
        Identity.GetWorkspaceKey().Returns(workspaceKey);
        WorkspacePermissionCache.GetUserPermissions(user.Id, workspaceKey).Returns(new UserPermissions
        {
            Permissions = [],
            Role = WorkspaceRole.Owner,
            UserId = user.Id,
            WorkspaceKey = workspaceKey,
        });

        var result = await Handler.Handle(new GetUserByEmailQuery("email"), CancellationToken.None);

        result.Should().BeEquivalentTo(new UserViewModel
        {
            Id = user.Id,
            Firstname = user.Firstname,
            Lastname = user.Lastname,
            PictureUrl = user.PictureUrl,
            DisplayName = user.DisplayName,
            Email = user.Email!,
            UserName = user.UserName!,
            LastLoginTime = user.LastLoginTime,
            RegistrationDate = user.RegistrationDate,
            Permissions = [],
        });
    }

    [Fact]
    public async Task GetByEmail_ShouldReturnNull_WhenUserNotFound()
    {
        UnitOfWork.Users.GetByEmail("email", Arg.Any<bool>()).ReturnsNull();

        var result = await Handler.Handle(new GetUserByEmailQuery("email"), CancellationToken.None);

        result.Should().BeNull();
    }
}

public class GetAllUsersQueryHandlerTests
{
    private readonly GetAllUsersQueryHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();

    public GetAllUsersQueryHandlerTests()
    {
        Handler = new(UnitOfWork);
    }

    [Fact]
    public async Task GetAll_ShouldReturnCorrectly_WhenInputValid()
    {
        UnitOfWork.Users.GetAllAsync().Returns(new List<AppUser> { AutoFixtures.AppUser });

        var result = await Handler.Handle(new GetAllUsersQuery(), CancellationToken.None);

        result.Should().NotBeEmpty();
    }
}

public class GetWorkspaceUsersQueryHandlerTests
{
    private readonly GetWorkspaceUsersQueryHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();

    public GetWorkspaceUsersQueryHandlerTests()
    {
        Handler = new(UnitOfWork, Identity);
    }

    [Fact]
    public async Task GetWorkspaceUsers_ShouldReturnCorrectly_WhenInputValid()
    {
        const string workspaceKey = "workspaceKey";
        var users = new List<WorkspaceAppUser> { AutoFixtures.WorkspaceAppUser };

        Identity.GetWorkspaceKey().Returns(workspaceKey);
        UnitOfWork.Users.GetWorkspaceAppUsers(workspaceKey, Arg.Any<bool>()).Returns(users);

        var result = await Handler.Handle(new GetWorkspaceUsersQuery(), CancellationToken.None);

        result.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetWorkspaceUsers_ShouldReturnEmpty_WhenNoUsers()
    {
        const string workspaceKey = "workspaceKey";

        Identity.GetWorkspaceKey().Returns(workspaceKey);
        UnitOfWork.Users.GetWorkspaceAppUsers(workspaceKey, Arg.Any<bool>()).Returns([]);

        var result = await Handler.Handle(new GetWorkspaceUsersQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }
}

public class InviteUsersToWorkspaceCommandHandlerTests
{
    private readonly InviteUsersToWorkspaceCommandHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();
    private readonly IEmailService Email = Substitute.For<IEmailService>();
    private readonly IHostingService Hosting = Substitute.For<IHostingService>();
    private readonly IInviteCache InviteCache = Substitute.For<IInviteCache>();
    private readonly IActivityLogger Activity = Substitute.For<IActivityLogger>();

    public InviteUsersToWorkspaceCommandHandlerTests()
    {
        Handler = new(UnitOfWork, Identity, Email, Hosting, InviteCache, Activity);
    }

    [Fact]
    public async Task InviteUsersToWorkspace_ShouldReturnCorrectly_WhenInputValid()
    {
        const string workspaceKey = "workspaceKey";
        var workspace = AutoFixtures.Workspace;
        var workspaceAppUsers = new List<WorkspaceAppUser> { AutoFixtures.WorkspaceAppUser };
        var users = new List<AppUser> { AutoFixtures.AppUser };
        var existingUsers = new List<AppUser> { new() { Id = "userId", Email = "existinguser@email.com" } };

        Identity.GetWorkspaceKey().Returns(workspaceKey);
        UnitOfWork.Users.InviteUsersToWorkspace(Arg.Any<IEnumerable<string>>(), Arg.Any<int>()).Returns(workspaceAppUsers);
        UnitOfWork.Users.GetByEmailRange(Arg.Any<IEnumerable<string>>(), Arg.Any<bool>()).Returns(users);
        UnitOfWork.Users.IsUserInWorkspaceRange(Arg.Any<IEnumerable<string>>(), Arg.Any<int>()).Returns(existingUsers);
        UnitOfWork.Workspaces.GetBySlug(workspaceKey, Arg.Any<bool>()).Returns(workspace);

        var result = await Handler.Handle(new InviteUsersToWorkspaceCommand(new List<string> { "user@email.com" }), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task InviteUsersToWorkspace_ReturnsFailure_WhenWorkspaceNotFound()
    {
        const string workspaceKey = "workspaceKey";
        var workspaceAppUsers = new List<WorkspaceAppUser> { AutoFixtures.WorkspaceAppUser };
        var users = new List<AppUser> { AutoFixtures.AppUser };
        var existingUsers = new List<AppUser> { new() { Id = "userId", Email = "existinguser@email.com" } };

        Identity.GetWorkspaceKey().Returns(workspaceKey);
        UnitOfWork.Users.InviteUsersToWorkspace(Arg.Any<IEnumerable<string>>(), Arg.Any<int>()).Returns(workspaceAppUsers);
        UnitOfWork.Users.GetByEmailRange(Arg.Any<IEnumerable<string>>(), Arg.Any<bool>()).Returns(users);
        UnitOfWork.Users.IsUserInWorkspaceRange(Arg.Any<IEnumerable<string>>(), Arg.Any<int>()).Returns(existingUsers);
        UnitOfWork.Workspaces.GetBySlug(workspaceKey, Arg.Any<bool>()).ReturnsNull();

        var result = await Handler.Handle(new InviteUsersToWorkspaceCommand(new List<string> { "user@email.com" }), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task InviteUsersToWorkspace_ReturnsFailure_WhenInputEmpty()
    {
        const string workspaceKey = "workspaceKey";
        var workspace = AutoFixtures.Workspace;

        Identity.GetWorkspaceKey().Returns(workspaceKey);
        UnitOfWork.Workspaces.GetBySlug(workspaceKey, Arg.Any<bool>()).Returns(workspace);

        var result = await Handler.Handle(new InviteUsersToWorkspaceCommand(new List<string>()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task InviteUsersToWorkspace_ShouldSendEmails_WhenInputValid()
    {
        const string workspaceKey = "workspaceKey";
        var workspace = AutoFixtures.Workspace;
        var workspaceAppUsers = new List<WorkspaceAppUser> { AutoFixtures.WorkspaceAppUser };
        var users = new List<AppUser> { AutoFixtures.AppUser };
        var existingUsers = new List<AppUser> { new() { Id = "userId", Email = "existinguser@email.com" } };

        Identity.GetWorkspaceKey().Returns(workspaceKey);
        UnitOfWork.Users.InviteUsersToWorkspace(Arg.Any<IEnumerable<string>>(), Arg.Any<int>()).Returns(workspaceAppUsers);
        UnitOfWork.Users.GetByEmailRange(Arg.Any<IEnumerable<string>>(), Arg.Any<bool>()).Returns(users);
        UnitOfWork.Users.IsUserInWorkspaceRange(Arg.Any<IEnumerable<string>>(), Arg.Any<int>()).Returns(existingUsers);
        UnitOfWork.Workspaces.GetBySlug(workspaceKey, Arg.Any<bool>()).Returns(workspace);

        await Handler.Handle(new InviteUsersToWorkspaceCommand(new List<string> { "user@email.com" }), CancellationToken.None);

        await Email.Received(1).Send(Arg.Any<SendMultipleEmailModel>());
    }

    [Fact]
    public async Task InviteUsersToWorkspace_ShouldNotInvite_ExistingUsers()
    {
        const string workspaceKey = "workspaceKey";
        var workspace = AutoFixtures.Workspace;
        var workspaceAppUsers = new List<WorkspaceAppUser> { AutoFixtures.WorkspaceAppUser };
        var users = new List<AppUser> { new() { Id = "userId", Email = "user@email.com" } };
        var existingUsers = new List<AppUser> { new() { Id = "userId", Email = "existinguser@email.com" } };

        Identity.GetWorkspaceKey().Returns(workspaceKey);
        UnitOfWork.Users.InviteUsersToWorkspace(Arg.Any<IEnumerable<string>>(), Arg.Any<int>()).Returns(workspaceAppUsers);
        UnitOfWork.Users.GetByEmailRange(Arg.Any<IEnumerable<string>>(), Arg.Any<bool>()).Returns(users);
        UnitOfWork.Users.IsUserInWorkspaceRange(Arg.Any<IEnumerable<string>>(), Arg.Any<int>()).Returns(existingUsers);
        UnitOfWork.Workspaces.GetBySlug(workspaceKey, Arg.Any<bool>()).Returns(workspace);

        var result = await Handler.Handle(new InviteUsersToWorkspaceCommand(new List<string> { "user@email.com", "existinguser@email.com" }), CancellationToken.None);

        result.Payload?.Emails.Should().Equal(new List<string> { "user@email.com" });
    }

    [Fact]
    public async Task InviteUsersToWorkspace_ShouldCallCompleteAsync_WhenValidId()
    {
        const string workspaceKey = "workspaceKey";
        var workspace = AutoFixtures.Workspace;
        var workspaceAppUsers = new List<WorkspaceAppUser> { AutoFixtures.WorkspaceAppUser };
        var users = new List<AppUser> { AutoFixtures.AppUser };
        var existingUsers = new List<AppUser> { new() { Id = "userId", Email = "existinguser@email.com" } };

        Identity.GetWorkspaceKey().Returns(workspaceKey);
        UnitOfWork.Users.InviteUsersToWorkspace(Arg.Any<IEnumerable<string>>(), Arg.Any<int>()).Returns(workspaceAppUsers);
        UnitOfWork.Users.GetByEmailRange(Arg.Any<IEnumerable<string>>(), Arg.Any<bool>()).Returns(users);
        UnitOfWork.Users.IsUserInWorkspaceRange(Arg.Any<IEnumerable<string>>(), Arg.Any<int>()).Returns(existingUsers);
        UnitOfWork.Workspaces.GetBySlug(workspaceKey, Arg.Any<bool>()).Returns(workspace);

        await Handler.Handle(new InviteUsersToWorkspaceCommand(new List<string> { "user@email.com" }), CancellationToken.None);

        await UnitOfWork.Received(1).CompleteAsync();
    }
}

public class RemoveUsersFromWorkspaceCommandHandlerTests
{
    private readonly RemoveUsersFromWorkspaceCommandHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();
    private readonly IWorkspaceUserCache Cache = Substitute.For<IWorkspaceUserCache>();
    private readonly IActivityLogger Activity = Substitute.For<IActivityLogger>();
    private readonly Fixture Fixture = new();

    public RemoveUsersFromWorkspaceCommandHandlerTests()
    {
        Handler = new(UnitOfWork, Identity, Cache, Activity);
    }

    [Fact]
    public async Task RemoveUsersFromWorkspace_ShouldReturnCorrectly_WhenInputValid()
    {
        const string workspaceKey = "workspaceKey";
        var workspace = AutoFixtures.Workspace;
        var workspaceAppUsers = new List<WorkspaceAppUser>
        {
            AutoFixtures.WorkspaceAppUser with { User = new() { Email = "user@email.com" } },
        };
        var users = new List<AppUser> { AutoFixtures.AppUser };

        Identity.GetWorkspaceKey().Returns(workspaceKey);
        UnitOfWork.Users.GetByEmailRange(Arg.Any<IEnumerable<string>>(), Arg.Any<bool>()).Returns(users);
        UnitOfWork.Users.RemoveUsersFromWorkspace(Arg.Any<IEnumerable<string>>(), Arg.Any<int>()).Returns(workspaceAppUsers);
        UnitOfWork.Workspaces.GetBySlug(workspaceKey, Arg.Any<bool>()).Returns(workspace);

        var result = await Handler.Handle(new RemoveUsersFromWorkspaceCommand(new List<string> { "user@email.com" }), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Payload!.Emails.Should().BeEquivalentTo(new List<string> { "user@email.com" });
    }

    [Fact]
    public async Task RemoveUsersFromWorkspace_ShouldRemoveUsersFromCache()
    {
        const string workspaceKey = "workspaceKey";
        var workspace = AutoFixtures.Workspace;
        var workspaceAppUsers = new List<WorkspaceAppUser>
        {
            AutoFixtures.WorkspaceAppUser with { User = new() { Email = "user@email.com" } },
        };

        var user = AutoFixtures.AppUserFixture
            .With(x => x.Id, "userId")
            .With(x => x.Email, "user@email.com")
            .Create();
        var users = new List<AppUser> { user };

        Identity.GetWorkspaceKey().Returns(workspaceKey);
        UnitOfWork.Users.GetByEmailRange(Arg.Any<IEnumerable<string>>(), Arg.Any<bool>()).Returns(users);
        UnitOfWork.Users.RemoveUsersFromWorkspace(Arg.Any<IEnumerable<string>>(), Arg.Any<int>()).Returns(workspaceAppUsers);
        UnitOfWork.Workspaces.GetBySlug(workspaceKey, Arg.Any<bool>()).Returns(workspace);

        await Handler.Handle(new RemoveUsersFromWorkspaceCommand(new List<string> { "user@email.com" }), CancellationToken.None);

        var key = new WorkspaceUserKey { UserId = user.Id, WorkspaceKey = workspaceKey };
        Cache.Received(1).Remove(Arg.Is<WorkspaceUserKey>(k => k == key));
    }

    [Fact]
    public async Task RemoveUsersFromWorkspace_ReturnsFailure_WhenWorkspaceNotFound()
    {
        const string workspaceKey = "workspaceKey";
        var workspaceAppUsers = new List<WorkspaceAppUser> { AutoFixtures.WorkspaceAppUser with { User = AutoFixtures.AppUser } };
        var users = new List<AppUser> { AutoFixtures.AppUser };

        Identity.GetWorkspaceKey().Returns(workspaceKey);
        UnitOfWork.Users.GetByEmailRange(Arg.Any<IEnumerable<string>>(), Arg.Any<bool>()).Returns(users);
        UnitOfWork.Users.RemoveUsersFromWorkspace(Arg.Any<IEnumerable<string>>(), Arg.Any<int>()).Returns(workspaceAppUsers);
        UnitOfWork.Workspaces.GetBySlug(workspaceKey, Arg.Any<bool>()).ReturnsNull();

        var result = await Handler.Handle(new RemoveUsersFromWorkspaceCommand(new List<string> { "user@email.com" }), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task RemoveUsersFromWorkspace_ReturnsFailure_WhenInputEmpty()
    {
        const string workspaceKey = "workspaceKey";
        var workspace = AutoFixtures.Workspace;

        Identity.GetWorkspaceKey().Returns(workspaceKey);
        UnitOfWork.Workspaces.GetBySlug(workspaceKey, Arg.Any<bool>()).Returns(workspace);

        var result = await Handler.Handle(new RemoveUsersFromWorkspaceCommand(new List<string>()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task RemoveUsersFromWorkspace_ReturnsFailure_WhenRemovingWorkspaceOwner()
    {
        const string workspaceKey = "workspaceKey";
        var workspace = AutoFixtures.Workspace with { OwnerId = "userId" };
        var workspaceAppUsers = new List<WorkspaceAppUser> { AutoFixtures.WorkspaceAppUser with { User = AutoFixtures.AppUser } };
        var users = new List<AppUser> { new() { Id = "userId", Email = "user@email.com" } };

        Identity.GetWorkspaceKey().Returns(workspaceKey);
        UnitOfWork.Users.GetByEmailRange(Arg.Any<IEnumerable<string>>(), Arg.Any<bool>()).Returns(users);
        UnitOfWork.Users.RemoveUsersFromWorkspace(Arg.Any<IEnumerable<string>>(), Arg.Any<int>()).Returns(workspaceAppUsers);
        UnitOfWork.Workspaces.GetBySlug(workspaceKey, Arg.Any<bool>()).Returns(workspace);

        var result = await Handler.Handle(new RemoveUsersFromWorkspaceCommand(new List<string> { "user@email.com" }), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task RemoveUsersFromWorkspace_ShouldCallCompleteAsync_WhenValidId()
    {
        const string workspaceKey = "workspaceKey";
        var workspace = AutoFixtures.Workspace;
        var workspaceAppUsers = new List<WorkspaceAppUser> { AutoFixtures.WorkspaceAppUser with { User = AutoFixtures.AppUser } };
        var users = new List<AppUser> { AutoFixtures.AppUser };

        Identity.GetWorkspaceKey().Returns(workspaceKey);
        UnitOfWork.Users.GetByEmailRange(Arg.Any<IEnumerable<string>>(), Arg.Any<bool>()).Returns(users);
        UnitOfWork.Users.RemoveUsersFromWorkspace(Arg.Any<IEnumerable<string>>(), Arg.Any<int>()).Returns(workspaceAppUsers);
        UnitOfWork.Workspaces.GetBySlug(workspaceKey, Arg.Any<bool>()).Returns(workspace);

        await Handler.Handle(new RemoveUsersFromWorkspaceCommand(new List<string> { "user@email.com" }), CancellationToken.None);

        await UnitOfWork.Received(1).CompleteAsync();
    }
}

public class UpdateUserCommandHandlerTests
{
    private readonly Fixture Fixture = new();
    private readonly UpdateUserCommandHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();

    public UpdateUserCommandHandlerTests()
    {
        Handler = new(UnitOfWork);
    }

    [Fact]
    public async Task Update_ShouldReturnCorrectly_WhenInputValid()
    {
        var request = Fixture.Build<UpdateUserRequest>().Create();
        var user = AutoFixtures.AppUser;

        UnitOfWork.Users.GetAsync(Arg.Any<string>()).Returns(user);

        var result = await Handler.Handle(new UpdateUserCommand(request), CancellationToken.None);

        result.Should().NotBeNull();
        result.Payload.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Payload!.Firstname.Should().Be(request.Firstname);
        result.Payload!.Lastname.Should().Be(request.Lastname);
        result.Payload!.PictureUrl.Should().Be(request.PictureUrl);
    }

    [Fact]
    public async Task Update_ShouldCallCompleteAsync_WhenInputValid()
    {
        var request = Fixture.Build<UpdateUserRequest>().Create();
        UnitOfWork.Users.GetAsync(Arg.Any<string>()).Returns(AutoFixtures.AppUser);

        await Handler.Handle(new UpdateUserCommand(request), CancellationToken.None);

        await UnitOfWork.Received(1).CompleteAsync();
    }

    [Fact]
    public async Task Update_ShouldReturnFailure_WhenUserNotFound()
    {
        var request = Fixture.Build<UpdateUserRequest>().Create();
        UnitOfWork.Users.GetAsync(Arg.Any<string>()).ReturnsNull();

        var result = await Handler.Handle(new UpdateUserCommand(request), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }
}

public class ToggleUserPermissionCommandHandlerTests
{
    private readonly ToggleUserPermissionCommandHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();
    private readonly IWorkspacePermissionCache WorkspacePermissionCache = Substitute.For<IWorkspacePermissionCache>();
    private readonly IActivityLogger Activity = Substitute.For<IActivityLogger>();

    public ToggleUserPermissionCommandHandlerTests()
    {
        Handler = new(UnitOfWork, Identity, WorkspacePermissionCache, Activity);
    }

    [Fact]
    public async Task ToggleUserPermission_ShouldAddPermission_WhenNotAlreadyGranted()
    {
        const string workspaceKey = "workspaceKey";
        const string userId = "userId";
        const string permission = "tasks.read";
        var workspace = AutoFixtures.Workspace;
        var userPermissions = new UserPermissions
        {
            UserId = userId, WorkspaceKey = workspaceKey, Role = WorkspaceRole.Member, Permissions = [],
        };

        Identity.GetWorkspaceKey().Returns(workspaceKey);
        UnitOfWork.Workspaces.GetBySlug(workspaceKey, Arg.Any<bool>()).Returns(workspace);
        UnitOfWork.WorkspaceUsers.GetUserPermissions(userId, workspaceKey, false).Returns(userPermissions);

        var request = new ToggleUserPermissionRequest { UserId = userId, Permission = permission };
        var result = await Handler.Handle(new ToggleUserPermissionCommand(request), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Payload.Should().Contain(permission);
    }

    [Fact]
    public async Task ToggleUserPermission_ShouldRemovePermission_WhenAlreadyGranted()
    {
        const string workspaceKey = "workspaceKey";
        const string userId = "userId";
        const string permission = "tasks.read";
        var workspace = AutoFixtures.Workspace;
        var userPermissions = new UserPermissions
        {
            UserId = userId, WorkspaceKey = workspaceKey, Role = WorkspaceRole.Member, Permissions = [permission],
        };

        Identity.GetWorkspaceKey().Returns(workspaceKey);
        UnitOfWork.Workspaces.GetBySlug(workspaceKey, Arg.Any<bool>()).Returns(workspace);
        UnitOfWork.WorkspaceUsers.GetUserPermissions(userId, workspaceKey, false).Returns(userPermissions);

        var request = new ToggleUserPermissionRequest { UserId = userId, Permission = permission };
        var result = await Handler.Handle(new ToggleUserPermissionCommand(request), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Payload.Should().NotContain(permission);
    }

    [Fact]
    public async Task ToggleUserPermission_ShouldReturnFailure_WhenWorkspaceNotFound()
    {
        const string workspaceKey = "workspaceKey";
        Identity.GetWorkspaceKey().Returns(workspaceKey);
        UnitOfWork.Workspaces.GetBySlug(workspaceKey, Arg.Any<bool>()).ReturnsNull();

        var request = new ToggleUserPermissionRequest { UserId = "userId", Permission = "tasks.read" };
        var result = await Handler.Handle(new ToggleUserPermissionCommand(request), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task ToggleUserPermission_ShouldReturnFailure_WhenUserNotInWorkspace()
    {
        const string workspaceKey = "workspaceKey";
        var workspace = AutoFixtures.Workspace;

        Identity.GetWorkspaceKey().Returns(workspaceKey);
        UnitOfWork.Workspaces.GetBySlug(workspaceKey, Arg.Any<bool>()).Returns(workspace);
        UnitOfWork.WorkspaceUsers.GetUserPermissions(Arg.Any<string>(), workspaceKey, false).ReturnsNull();

        var request = new ToggleUserPermissionRequest { UserId = "userId", Permission = "tasks.read" };
        var result = await Handler.Handle(new ToggleUserPermissionCommand(request), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task ToggleUserPermission_ShouldCallSetUserPermissions_WhenSuccessful()
    {
        const string workspaceKey = "workspaceKey";
        const string userId = "userId";
        var workspace = AutoFixtures.Workspace;
        var userPermissions = new UserPermissions
        {
            UserId = userId, WorkspaceKey = workspaceKey, Role = WorkspaceRole.Member, Permissions = [],
        };

        Identity.GetWorkspaceKey().Returns(workspaceKey);
        UnitOfWork.Workspaces.GetBySlug(workspaceKey, Arg.Any<bool>()).Returns(workspace);
        UnitOfWork.WorkspaceUsers.GetUserPermissions(userId, workspaceKey, false).Returns(userPermissions);

        var request = new ToggleUserPermissionRequest { UserId = userId, Permission = "tasks.read" };
        await Handler.Handle(new ToggleUserPermissionCommand(request), CancellationToken.None);

        await UnitOfWork.WorkspaceUsers.Received(1).SetUserPermissions(userId, workspace.Id, Arg.Any<IEnumerable<string>>());
    }

    [Fact]
    public async Task ToggleUserPermission_ShouldCallCompleteAsync_WhenSuccessful()
    {
        const string workspaceKey = "workspaceKey";
        const string userId = "userId";
        var workspace = AutoFixtures.Workspace;
        var userPermissions = new UserPermissions
        {
            UserId = userId, WorkspaceKey = workspaceKey, Role = WorkspaceRole.Member, Permissions = [],
        };

        Identity.GetWorkspaceKey().Returns(workspaceKey);
        UnitOfWork.Workspaces.GetBySlug(workspaceKey, Arg.Any<bool>()).Returns(workspace);
        UnitOfWork.WorkspaceUsers.GetUserPermissions(userId, workspaceKey, false).Returns(userPermissions);

        var request = new ToggleUserPermissionRequest { UserId = userId, Permission = "tasks.read" };
        await Handler.Handle(new ToggleUserPermissionCommand(request), CancellationToken.None);

        await UnitOfWork.Received(1).CompleteAsync();
    }

    [Fact]
    public async Task ToggleUserPermission_ShouldClearPermissionCache_WhenSuccessful()
    {
        const string workspaceKey = "workspaceKey";
        const string userId = "userId";
        var workspace = AutoFixtures.Workspace;
        var userPermissions = new UserPermissions
        {
            UserId = userId, WorkspaceKey = workspaceKey, Role = WorkspaceRole.Member, Permissions = [],
        };

        Identity.GetWorkspaceKey().Returns(workspaceKey);
        UnitOfWork.Workspaces.GetBySlug(workspaceKey, Arg.Any<bool>()).Returns(workspace);
        UnitOfWork.WorkspaceUsers.GetUserPermissions(userId, workspaceKey, false).Returns(userPermissions);

        var request = new ToggleUserPermissionRequest { UserId = userId, Permission = "tasks.read" };
        await Handler.Handle(new ToggleUserPermissionCommand(request), CancellationToken.None);

        var expectedKey = new WorkspaceUserKey { UserId = userId, WorkspaceKey = workspaceKey };
        WorkspacePermissionCache.Received(1).Remove(Arg.Is<WorkspaceUserKey>(k => k == expectedKey));
    }
}
