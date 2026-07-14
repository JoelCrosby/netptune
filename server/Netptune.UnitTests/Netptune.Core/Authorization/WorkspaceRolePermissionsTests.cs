using FluentAssertions;
using Netptune.Core.Authorization;
using Xunit;

namespace Netptune.UnitTests.Netptune.Core.Authorization;

public class WorkspaceRolePermissionsTests
{
    [Fact]
    public void Owner_ShouldHaveEveryAvailablePermission()
    {
        var ownerPermissions = WorkspaceRolePermissions.GetDefaultPermissions(WorkspaceRole.Owner);

        ownerPermissions.Should().BeEquivalentTo(NetptunePermissions.All);
    }
}
