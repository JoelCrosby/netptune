using FluentAssertions;

using Netptune.Core.Authorization;

using Xunit;

namespace Netptune.UnitTests.Netptune.Core.Authorization;

public class AssistantPermissionTests
{
    [Theory]
    [InlineData(WorkspaceRole.Owner)]
    [InlineData(WorkspaceRole.Admin)]
    public void ReadAllConversations_ShouldBeGranted_ToWorkspaceAdministrators(WorkspaceRole role)
    {
        var permissions = WorkspaceRolePermissions.GetDefaultPermissions(role);

        permissions.Should().Contain(NetptunePermissions.Assistant.ReadAllConversations);
    }

    [Theory]
    [InlineData(WorkspaceRole.Member)]
    [InlineData(WorkspaceRole.Viewer)]
    public void ReadAllConversations_ShouldNotBeGranted_ToOrdinaryMembers(WorkspaceRole role)
    {
        var permissions = WorkspaceRolePermissions.GetDefaultPermissions(role);

        permissions.Should().NotContain(
            NetptunePermissions.Assistant.ReadAllConversations,
            "a member reading every colleague's assistant conversation is a privacy breach");
    }

    [Fact]
    public void ReadAllConversations_ShouldBeAKnownPermission()
    {
        NetptunePermissions.All.Should().Contain(NetptunePermissions.Assistant.ReadAllConversations);
    }
}
