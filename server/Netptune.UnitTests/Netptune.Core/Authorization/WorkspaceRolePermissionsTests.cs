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

    [Fact]
    public void FilePermissions_ShouldMatchRoleDefaults()
    {
        WorkspaceRolePermissions.GetDefaultPermissions(WorkspaceRole.Viewer)
            .Should().Contain(NetptunePermissions.Files.Read)
            .And.NotContain(NetptunePermissions.Files.Upload);

        WorkspaceRolePermissions.GetDefaultPermissions(WorkspaceRole.Member)
            .Should().Contain([NetptunePermissions.Files.Read, NetptunePermissions.Files.Upload, NetptunePermissions.Files.DeleteOwn])
            .And.NotContain(NetptunePermissions.Storage.Read);

        WorkspaceRolePermissions.GetDefaultPermissions(WorkspaceRole.Admin)
            .Should().Contain([NetptunePermissions.Files.DeleteAny, NetptunePermissions.Storage.Read, NetptunePermissions.Storage.Manage]);
    }

    [Fact]
    public void FlagPermissions_ShouldMatchRoleDefaults()
    {
        WorkspaceRolePermissions.GetDefaultPermissions(WorkspaceRole.Viewer)
            .Should().Contain(NetptunePermissions.Flags.Read)
            .And.NotContain(NetptunePermissions.Flags.Resolve);

        WorkspaceRolePermissions.GetDefaultPermissions(WorkspaceRole.Member)
            .Should().Contain([NetptunePermissions.Flags.Read, NetptunePermissions.Flags.Resolve]);
    }
}
