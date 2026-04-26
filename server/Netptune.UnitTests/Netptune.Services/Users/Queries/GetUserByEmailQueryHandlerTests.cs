using FluentAssertions;

using Netptune.Core.Authorization;
using Netptune.Core.Cache;
using Netptune.Core.Models;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Users;
using Netptune.Services.Users.Queries.GetUserByEmail;

using NSubstitute;
using NSubstitute.ReturnsExtensions;

using Xunit;

namespace Netptune.UnitTests.Netptune.Services.Users.Queries;

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
