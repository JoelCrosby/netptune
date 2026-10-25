using FluentAssertions;

using Netptune.Core.Authorization;
using Netptune.Core.Models;

using Xunit;

namespace Netptune.UnitTests.Netptune.Core.Models;

public class UserPermissionsTests
{
    private static readonly string[] Required = [NetptunePermissions.Tasks.Read, NetptunePermissions.Tasks.Update];

    [Theory]
    [InlineData(WorkspaceRole.Owner)]
    [InlineData(WorkspaceRole.Admin)]
    public void Allows_ShouldPassOwnersAndAdmins_WithoutTheirOwnPermissions(WorkspaceRole role)
    {
        var membership = Membership(role, []);

        membership.Allows(Required).Should().BeTrue();
    }

    [Fact]
    public void Allows_ShouldNeedEveryPermission_ForAMember()
    {
        var withBoth = Membership(WorkspaceRole.Member, Required);
        var withOne = Membership(WorkspaceRole.Member, [NetptunePermissions.Tasks.Read]);

        withBoth.Allows(Required).Should().BeTrue();
        withOne.Allows(Required).Should().BeFalse();
    }

    private static UserPermissions Membership(WorkspaceRole role, IEnumerable<string> permissions)
    {
        return new UserPermissions
        {
            UserId = "user",
            WorkspaceKey = "workspace",
            Role = role,
            Permissions = permissions.ToHashSet(),
        };
    }
}
