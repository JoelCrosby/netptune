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

    [Theory]
    [InlineData(WorkspaceRole.Owner)]
    [InlineData(WorkspaceRole.Admin)]
    [InlineData(WorkspaceRole.Member)]
    public void UseWeb_ShouldBeGranted_FromMemberUpwards(WorkspaceRole role)
    {
        var permissions = WorkspaceRolePermissions.GetDefaultPermissions(role);

        permissions.Should().Contain(NetptunePermissions.Assistant.UseWeb);
    }

    [Fact]
    public void UseWeb_ShouldNotBeGranted_ToViewers()
    {
        var permissions = WorkspaceRolePermissions.GetDefaultPermissions(WorkspaceRole.Viewer);

        permissions.Should().NotContain(
            NetptunePermissions.Assistant.UseWeb,
            "outbound fetches leave the workspace and spend the caller's provider budget");
    }

    [Fact]
    public void UseWeb_ShouldBeAKnownPermission()
    {
        NetptunePermissions.All.Should().Contain(NetptunePermissions.Assistant.UseWeb);
    }

    [Fact]
    public void UseWeb_ShouldNotBeReadableByThePublic()
    {
        NetptunePermissions.PublicReadable.Should().NotContain(
            NetptunePermissions.Assistant.UseWeb,
            "an anonymous visitor to a public workspace must not be able to drive server-side fetches");
    }
}
