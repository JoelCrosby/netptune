using AutoFixture;

using FluentAssertions;

using Netptune.Core.Cache;
using Netptune.Core.Entities;
using Netptune.Core.Messaging;
using Netptune.Core.Relationships;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Users;
using Netptune.Services;

using NSubstitute;
using NSubstitute.ReturnsExtensions;

using Xunit;

namespace Netptune.UnitTests.Netptune.Services;

public class UserServiceTests
{
    private readonly Fixture Fixture = new();

    private readonly UserService Service;

    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();
    private readonly IWorkspaceUserCache Cache = Substitute.For<IWorkspaceUserCache>();
    private readonly IEmailService Email = Substitute.For<IEmailService>();
    private readonly IHostingService Hosting = Substitute.For<IHostingService>();
    private readonly IInviteCache InviteCache = Substitute.For<IInviteCache>();

    public UserServiceTests()
    {
        Service = new(
            UnitOfWork,
            Identity,
            Email,
            Hosting,
            Cache,
            InviteCache
        );
    }

    [Fact]
    public async Task Get_ShouldReturnCorrectly_WhenInputValid()
    {
        var user = AutoFixtures.AppUser;

        UnitOfWork.Users.GetAsync("userId", Arg.Any<bool>()).Returns(user);

        var result = await Service.Get("userId");

        result.Should().BeEquivalentTo(new UserViewModel
        {
            Id = user.Id,
            Firstname = user.Firstname,
            Lastname = user.Lastname,
            PictureUrl = user.PictureUrl,
            DisplayName = user.DisplayName,
            Email = user.Email,
            UserName = user.UserName,
            LastLoginTime = user.LastLoginTime,
            RegistrationDate = user.RegistrationDate,
        });
    }

    [Fact]
    public async Task Get_ShouldReturnNull_WhenUserNotFound()
    {
        UnitOfWork.Users.GetAsync("userId", Arg.Any<bool>()).ReturnsNull();

        var result = await Service.Get("userId");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByEmail_ShouldReturnCorrectly_WhenInputValid()
    {
        var user = AutoFixtures.AppUser;

        UnitOfWork.Users.GetByEmail("email", Arg.Any<bool>()).Returns(user);

        var result = await Service.GetByEmail("email");

        result.Should().BeEquivalentTo(new UserViewModel
        {
            Id = user.Id,
            Firstname = user.Firstname,
            Lastname = user.Lastname,
            PictureUrl = user.PictureUrl,
            DisplayName = user.DisplayName,
            Email = user.Email,
            UserName = user.UserName,
            LastLoginTime = user.LastLoginTime,
            RegistrationDate = user.RegistrationDate,
        });
    }

    [Fact]
    public async Task GetByEmail_ShouldReturnNull_WhenUserNotFound()
    {
        UnitOfWork.Users.GetByEmail("email", Arg.Any<bool>()).ReturnsNull();

        var result = await Service.GetByEmail("email");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAll_ShouldReturnCorrectly_WhenInputValid()
    {
        UnitOfWork.Users.GetAllAsync().Returns(new List<AppUser>{ AutoFixtures.AppUser });

        var result = await Service.GetAll();

        result.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetWorkspaceUsers_ShouldReturnCorrectly_WhenInputValid()
    {
        const string workspaceKey = "workspaceKey";

        var workspace = AutoFixtures.Workspace;
        var users = new List<AppUser> { AutoFixtures.AppUser };

        Identity.GetWorkspaceKey().Returns(workspaceKey);
        UnitOfWork.Users.GetWorkspaceUsers(workspaceKey, Arg.Any<bool>()).Returns(users);
        UnitOfWork.Workspaces.GetBySlug(workspaceKey, Arg.Any<bool>()).Returns(workspace);

        var result = await Service.GetWorkspaceUsers();

        result.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetWorkspaceUsers_ShouldReturnNull_WhenWorkspaceNotFound()
    {
        const string workspaceKey = "workspaceKey";

        var users = new List<AppUser> { AutoFixtures.AppUser };

        Identity.GetWorkspaceKey().Returns(workspaceKey);
        UnitOfWork.Users.GetWorkspaceUsers(workspaceKey, Arg.Any<bool>()).Returns(users);
        UnitOfWork.Workspaces.GetBySlug(workspaceKey, Arg.Any<bool>()).ReturnsNull();

        var result = await Service.GetWorkspaceUsers();

        result.Should().BeNull();
    }

    [Fact]
    public async Task InviteUsersToWorkspace_ShouldReturnCorrectly_WhenInputValid()
    {
        const string workspaceKey = "workspaceKey";

        var workspace = AutoFixtures.Workspace;
        var workspaceAppUsers = new List<WorkspaceAppUser> { AutoFixtures.WorkspaceAppUser };
        var users = new List<AppUser> { AutoFixtures.AppUser };

        Identity.GetWorkspaceKey().Returns(workspaceKey);
        UnitOfWork.Users.InviteUsersToWorkspace(Arg.Any<IEnumerable<string>>(), Arg.Any<int>()).Returns(workspaceAppUsers);
        UnitOfWork.Users.GetByEmailRange(Arg.Any<IEnumerable<string>>(), Arg.Any<bool>()).Returns(users);
        UnitOfWork.Users.IsUserInWorkspaceRange(Arg.Any<IEnumerable<string>>(), Arg.Any<int>()).Returns(new List<string> { "userId" });
        UnitOfWork.Workspaces.GetBySlug(workspaceKey, Arg.Any<bool>()).Returns(workspace);

        var emails = new List<string> { "user@email.com" };
        var result = await Service.InviteUsersToWorkspace(emails);

        result.IsSuccess.Should().BeTrue();
    }
}
