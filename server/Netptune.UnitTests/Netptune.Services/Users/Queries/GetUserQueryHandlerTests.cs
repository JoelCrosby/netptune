using FluentAssertions;

using Netptune.Core.Authorization;
using Netptune.Core.Cache;
using Netptune.Core.Models;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Users;
using Netptune.Services.Users.Queries;

using NSubstitute;
using NSubstitute.ReturnsExtensions;

using Xunit;

namespace Netptune.UnitTests.Netptune.Services.Users.Queries;

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

        UnitOfWork.Users.GetAsync("userId", Arg.Any<bool>(), TestContext.Current.CancellationToken).Returns(user);
        Identity.GetWorkspaceKey().Returns(workspaceKey);
        WorkspacePermissionCache.GetUserPermissions(user.Id, workspaceKey).Returns(new UserPermissions
        {
            Permissions = [],
            Role = WorkspaceRole.Owner,
            UserId = user.Id,
            WorkspaceKey = workspaceKey,
        });

        var result = await Handler.Handle(new GetUserQuery("userId"), TestContext.Current.CancellationToken);

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
        UnitOfWork.Users.GetAsync("userId", Arg.Any<bool>(), TestContext.Current.CancellationToken).ReturnsNull();

        var result = await Handler.Handle(new GetUserQuery("userId"), TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }
}
