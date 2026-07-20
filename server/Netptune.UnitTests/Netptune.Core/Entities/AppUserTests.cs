using FluentAssertions;

using Netptune.Core.Authorization;
using Netptune.Core.Entities;

using Xunit;

namespace Netptune.UnitTests.Netptune.Core.Entities;

public class AppUserTests
{
    [Theory]
    [InlineData(AppUserType.User, false)]
    [InlineData(AppUserType.ServiceAccount, true)]
    public void ToViewModel_SetsServiceAccountFlag(AppUserType userType, bool expected)
    {
        var user = CreateUser(userType);

        var result = user.ToViewModel();

        result.IsServiceAccount.Should().Be(expected);
    }

    [Theory]
    [InlineData(AppUserType.User, false)]
    [InlineData(AppUserType.ServiceAccount, true)]
    public void ToWorkspaceViewModel_SetsServiceAccountFlag(AppUserType userType, bool expected)
    {
        var user = CreateUser(userType);

        var result = user.ToWorkspaceViewModel(WorkspaceRole.Member);

        result.IsServiceAccount.Should().Be(expected);
    }

    private static AppUser CreateUser(AppUserType userType)
    {
        return new AppUser
        {
            Id = "user-one",
            Firstname = "Example",
            Lastname = "User",
            Email = "user@example.com",
            UserName = "user@example.com",
            UserType = userType,
        };
    }
}
