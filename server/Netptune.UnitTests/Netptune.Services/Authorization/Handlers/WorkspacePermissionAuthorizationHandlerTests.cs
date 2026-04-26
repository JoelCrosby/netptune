using System.Security.Claims;

using FluentAssertions;

using Microsoft.AspNetCore.Authorization;

using Netptune.Core.Authorization;
using Netptune.Core.Cache;
using Netptune.Core.Models;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Services.Authorization.Handlers;
using Netptune.Services.Authorization.Requirements;

using NSubstitute;
using NSubstitute.ReturnsExtensions;

using Xunit;

namespace Netptune.UnitTests.Netptune.Services.Authorization.Handlers;

public class WorkspacePermissionAuthorizationHandlerTests
{
    private readonly WorkspacePermissionResourceAuthorizationHandler Handler;

    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();
    private readonly IWorkspacePermissionCache Cache = Substitute.For<IWorkspacePermissionCache>();
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();

    private const string WorkspaceKey = "test-workspace";
    private const string UserId = "user-123";

    public WorkspacePermissionAuthorizationHandlerTests()
    {
        Handler = new WorkspacePermissionResourceAuthorizationHandler(Identity, Cache, UnitOfWork);
    }

    private static AuthorizationHandlerContext CreateContext(ClaimsPrincipal user, WorkspacePermissionRequirement requirement)
    {
        return new AuthorizationHandlerContext([requirement], user, null);
    }

    private static ClaimsPrincipal AuthenticatedUser() =>
        new(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, UserId)], "Bearer"));

    private static ClaimsPrincipal AnonymousUser() =>
        new(new ClaimsIdentity());

    private static UserPermissions MakePermissions(WorkspaceRole role, params string[] permissions) =>
        new()
        {
            UserId = UserId,
            WorkspaceKey = WorkspaceKey,
            Role = role,
            Permissions = [.. permissions],
        };

    // Authenticated — Owner/Admin bypass

    [Theory]
    [InlineData(WorkspaceRole.Owner)]
    [InlineData(WorkspaceRole.Admin)]
    public async Task HandleRequirement_ShouldSucceed_ForOwnerAndAdminRegardlessOfPermissions(WorkspaceRole role)
    {
        var user = AuthenticatedUser();
        var requirement = new WorkspacePermissionRequirement("tasks.delete_any");

        Identity.TryGetWorkspaceKey().Returns(WorkspaceKey);
        Identity.GetCurrentUserId().Returns(UserId);
        Cache.GetUserPermissions(UserId, WorkspaceKey).Returns(MakePermissions(role));

        var context = CreateContext(user, requirement);
        await Handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    // Authenticated — specific permission granted

    [Fact]
    public async Task HandleRequirement_ShouldSucceed_WhenMemberHasRequiredPermission()
    {
        var user = AuthenticatedUser();
        var requirement = new WorkspacePermissionRequirement(NetptunePermissions.Tasks.Read);

        Identity.TryGetWorkspaceKey().Returns(WorkspaceKey);
        Identity.GetCurrentUserId().Returns(UserId);
        Cache.GetUserPermissions(UserId, WorkspaceKey).Returns(MakePermissions(WorkspaceRole.Member, NetptunePermissions.Tasks.Read));

        var context = CreateContext(user, requirement);
        await Handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task HandleRequirement_ShouldFail_WhenMemberLacksRequiredPermission()
    {
        var user = AuthenticatedUser();
        var requirement = new WorkspacePermissionRequirement(NetptunePermissions.Tasks.Delete);

        Identity.TryGetWorkspaceKey().Returns(WorkspaceKey);
        Identity.GetCurrentUserId().Returns(UserId);
        Cache.GetUserPermissions(UserId, WorkspaceKey).Returns(MakePermissions(WorkspaceRole.Member, NetptunePermissions.Tasks.Read));

        var context = CreateContext(user, requirement);
        await Handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task HandleRequirement_ShouldFail_WhenUserPermissionsNotFound()
    {
        var user = AuthenticatedUser();
        var requirement = new WorkspacePermissionRequirement(NetptunePermissions.Tasks.Read);

        Identity.TryGetWorkspaceKey().Returns(WorkspaceKey);
        Identity.GetCurrentUserId().Returns(UserId);
        Cache.GetUserPermissions(UserId, WorkspaceKey).ReturnsNull();

        var context = CreateContext(user, requirement);
        await Handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    // Unauthenticated — public workspace read access

    [Fact]
    public async Task HandleRequirement_ShouldSucceed_ForAnonymousReadOnPublicWorkspace()
    {
        var user = AnonymousUser();
        var requirement = new WorkspacePermissionRequirement(NetptunePermissions.Tasks.Read);

        var workspace = AutoFixtures.Workspace with { IsPublic = true };

        Identity.TryGetWorkspaceKey().Returns(WorkspaceKey);
        UnitOfWork.Workspaces.GetBySlug(WorkspaceKey, isReadonly: true).Returns(workspace);

        var context = CreateContext(user, requirement);
        await Handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task HandleRequirement_ShouldFail_ForAnonymousReadOnPrivateWorkspace()
    {
        var user = AnonymousUser();
        var requirement = new WorkspacePermissionRequirement(NetptunePermissions.Tasks.Read);

        var workspace = AutoFixtures.Workspace with { IsPublic = false };

        Identity.TryGetWorkspaceKey().Returns(WorkspaceKey);
        UnitOfWork.Workspaces.GetBySlug(WorkspaceKey, isReadonly: true).Returns(workspace);

        var context = CreateContext(user, requirement);
        await Handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task HandleRequirement_ShouldFail_ForAnonymousWriteOnPublicWorkspace()
    {
        var user = AnonymousUser();
        var requirement = new WorkspacePermissionRequirement(NetptunePermissions.Tasks.Create);

        var workspace = AutoFixtures.Workspace with { IsPublic = true };

        Identity.TryGetWorkspaceKey().Returns(WorkspaceKey);
        UnitOfWork.Workspaces.GetBySlug(WorkspaceKey, isReadonly: true).Returns(workspace);

        var context = CreateContext(user, requirement);
        await Handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task HandleRequirement_ShouldNotQuery_WhenAnonymousAndNonReadPermission()
    {
        var user = AnonymousUser();
        var requirement = new WorkspacePermissionRequirement(NetptunePermissions.Tasks.Delete);

        Identity.TryGetWorkspaceKey().Returns(WorkspaceKey);

        var context = CreateContext(user, requirement);
        await Handler.HandleAsync(context);

        await UnitOfWork.Workspaces.DidNotReceive().GetBySlug(Arg.Any<string>(), Arg.Any<bool>());
    }

    [Fact]
    public async Task HandleRequirement_ShouldNotQuery_WhenAnonymousAndWorkspaceKeyIsNull()
    {
        var user = AnonymousUser();
        var requirement = new WorkspacePermissionRequirement(NetptunePermissions.Tasks.Read);

        Identity.TryGetWorkspaceKey().ReturnsNull();

        var context = CreateContext(user, requirement);
        await Handler.HandleAsync(context);

        await UnitOfWork.Workspaces.DidNotReceive().GetBySlug(Arg.Any<string>(), Arg.Any<bool>());
        context.HasSucceeded.Should().BeFalse();
    }
}
