using System.Security.Claims;

using FluentAssertions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

using Netptune.Core.Authorization;
using Netptune.Core.Cache;
using Netptune.Core.Models;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Identity.Authorization.Handlers;
using Netptune.Identity.Authorization.Requirements;

using NSubstitute;
using NSubstitute.ReturnsExtensions;

using Xunit;

namespace Netptune.UnitTests.Netptune.Identity.Authorization.Handlers;

public class WorkspacePermissionAuthorizationHandlerTests
{
    private readonly WorkspacePermissionResourceAuthorizationHandler Handler;

    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();
    private readonly IWorkspacePermissionCache Cache = Substitute.For<IWorkspacePermissionCache>();
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IHttpContextAccessor HttpContext = Substitute.For<IHttpContextAccessor>();

    private const string WorkspaceKey = "test-workspace";
    private const string UserId = "user-123";

    public WorkspacePermissionAuthorizationHandlerTests()
    {
        Handler = new WorkspacePermissionResourceAuthorizationHandler(Identity, Cache, UnitOfWork, HttpContext);

        RequestMethod(HttpMethods.Get);
    }

    private void RequestMethod(string method)
    {
        var httpContext = new DefaultHttpContext();

        httpContext.Request.Method = method;

        HttpContext.HttpContext.Returns(httpContext);
    }

    private static AuthorizationHandlerContext CreateContext(
        ClaimsPrincipal user,
        WorkspacePermissionRequirement requirement,
        object? resource = null)
    {
        return new AuthorizationHandlerContext([requirement], user, resource);
    }

    private static ClaimsPrincipal AuthenticatedUser() =>
        new(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, UserId)], "Bearer"));

    private static ClaimsPrincipal AnonymousUser() => new(new ClaimsIdentity());

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
    public async Task HandleRequirement_ShouldAuthorizeTheExplicitWorkspaceResource_InsteadOfTheHeader()
    {
        const string requestedWorkspace = "new-workspace";
        var user = AuthenticatedUser();
        var requirement = new WorkspacePermissionRequirement(NetptunePermissions.Workspace.Read);

        Identity.TryGetWorkspaceKey().Returns("old-workspace");
        Identity.GetCurrentUserId().Returns(UserId);
        Cache.GetUserPermissions(UserId, requestedWorkspace)
            .Returns(MakePermissions(WorkspaceRole.Owner));

        var context = CreateContext(user, requirement, requestedWorkspace);
        await Handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
        await Cache.Received(1).GetUserPermissions(UserId, requestedWorkspace);
        await Cache.DidNotReceive().GetUserPermissions(UserId, "old-workspace");
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

    [Fact]
    public async Task HandleRequirement_ShouldSucceed_WhenCredentialAndMembershipBothGrantPermission()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, UserId),
            new Claim(NetptuneClaims.CredentialId, Guid.NewGuid().ToString()),
            new Claim(NetptuneClaims.CredentialScope, NetptunePermissions.Tasks.Read),
        ], "ApiKey"));
        var requirement = new WorkspacePermissionRequirement(NetptunePermissions.Tasks.Read);

        Identity.TryGetWorkspaceKey().Returns(WorkspaceKey);
        Identity.GetCurrentUserId().Returns(UserId);
        Cache
            .GetUserPermissions(UserId, WorkspaceKey)
            .Returns(MakePermissions(WorkspaceRole.Member, NetptunePermissions.Tasks.Read));

        var context = CreateContext(user, requirement);
        await Handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task HandleRequirement_ShouldFail_WhenCredentialScopeDoesNotGrantMembershipPermission()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, UserId),
            new Claim(NetptuneClaims.CredentialId, Guid.NewGuid().ToString()),
            new Claim(NetptuneClaims.CredentialScope, NetptunePermissions.Tasks.Read),
        ], "ApiKey"));
        var requirement = new WorkspacePermissionRequirement(NetptunePermissions.Tasks.Delete);

        Identity.TryGetWorkspaceKey().Returns(WorkspaceKey);
        Identity.GetCurrentUserId().Returns(UserId);
        Cache
            .GetUserPermissions(UserId, WorkspaceKey)
            .Returns(MakePermissions(WorkspaceRole.Admin, NetptunePermissions.Tasks.Delete));

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
        UnitOfWork.Workspaces.GetBySlug(WorkspaceKey, isReadonly: true, cancellationToken: Arg.Any<CancellationToken>()).Returns(workspace);

        var context = CreateContext(user, requirement);
        await Handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task HandleRequirement_ShouldSucceed_ForAnonymousReadTheWorkspaceShares()
    {
        var user = AnonymousUser();
        var requirement = new WorkspacePermissionRequirement(NetptunePermissions.Tasks.Read);

        var workspace = AutoFixtures.Workspace with
        {
            IsPublic = true,
            PublicPermissions = [NetptunePermissions.Tasks.Read],
        };

        Identity.TryGetWorkspaceKey().Returns(WorkspaceKey);
        UnitOfWork.Workspaces.GetBySlug(WorkspaceKey, isReadonly: true, cancellationToken: Arg.Any<CancellationToken>()).Returns(workspace);

        var context = CreateContext(user, requirement);
        await Handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task HandleRequirement_ShouldFail_ForAnonymousReadTheWorkspaceDoesNotShare()
    {
        var user = AnonymousUser();
        var requirement = new WorkspacePermissionRequirement(NetptunePermissions.Sprints.Read);

        var workspace = AutoFixtures.Workspace with
        {
            IsPublic = true,
            PublicPermissions = [NetptunePermissions.Tasks.Read],
        };

        Identity.TryGetWorkspaceKey().Returns(WorkspaceKey);
        UnitOfWork.Workspaces.GetBySlug(WorkspaceKey, isReadonly: true, cancellationToken: Arg.Any<CancellationToken>()).Returns(workspace);

        var context = CreateContext(user, requirement);
        await Handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task HandleRequirement_ShouldFail_WhenTheWorkspaceSharesNothing()
    {
        var user = AnonymousUser();
        var requirement = new WorkspacePermissionRequirement(NetptunePermissions.Tasks.Read);

        var workspace = AutoFixtures.Workspace with
        {
            IsPublic = true,
            PublicPermissions = [],
        };

        Identity.TryGetWorkspaceKey().Returns(WorkspaceKey);
        UnitOfWork.Workspaces.GetBySlug(WorkspaceKey, isReadonly: true, cancellationToken: Arg.Any<CancellationToken>()).Returns(workspace);

        var context = CreateContext(user, requirement);
        await Handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task HandleRequirement_ShouldFail_WhenAStoredPermissionIsOutsideTheCeiling()
    {
        var user = AnonymousUser();
        var requirement = new WorkspacePermissionRequirement(NetptunePermissions.Audit.Read);

        var workspace = AutoFixtures.Workspace with
        {
            IsPublic = true,
            PublicPermissions = [NetptunePermissions.Audit.Read],
        };

        Identity.TryGetWorkspaceKey().Returns(WorkspaceKey);
        UnitOfWork.Workspaces.GetBySlug(WorkspaceKey, isReadonly: true, cancellationToken: Arg.Any<CancellationToken>()).Returns(workspace);

        var context = CreateContext(user, requirement);
        await Handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task HandleRequirement_ShouldFail_ForAnonymousReadOnPrivateWorkspace()
    {
        var user = AnonymousUser();
        var requirement = new WorkspacePermissionRequirement(NetptunePermissions.Tasks.Read);

        var workspace = AutoFixtures.Workspace with { IsPublic = false };

        Identity.TryGetWorkspaceKey().Returns(WorkspaceKey);
        UnitOfWork.Workspaces.GetBySlug(WorkspaceKey, isReadonly: true, cancellationToken: Arg.Any<CancellationToken>()).Returns(workspace);

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
        UnitOfWork.Workspaces.GetBySlug(WorkspaceKey, isReadonly: true, cancellationToken: Arg.Any<CancellationToken>()).Returns(workspace);

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

        await UnitOfWork.Workspaces.DidNotReceive().GetBySlug(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleRequirement_ShouldNotQuery_WhenAnonymousAndWorkspaceKeyIsNull()
    {
        var user = AnonymousUser();
        var requirement = new WorkspacePermissionRequirement(NetptunePermissions.Tasks.Read);

        Identity.TryGetWorkspaceKey().ReturnsNull();

        var context = CreateContext(user, requirement);
        await Handler.HandleAsync(context);

        await UnitOfWork.Workspaces.DidNotReceive().GetBySlug(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
        context.HasSucceeded.Should().BeFalse();
    }

    // Unauthenticated — public access is read-only whatever the route asks for

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("PATCH")]
    [InlineData("DELETE")]
    public async Task HandleRequirement_ShouldFail_ForAnonymousOnAWriteGatedByAReadPermission(string method)
    {
        var user = AnonymousUser();
        var requirement = new WorkspacePermissionRequirement(NetptunePermissions.Tasks.Read);

        var workspace = AutoFixtures.Workspace with { IsPublic = true };

        RequestMethod(method);
        Identity.TryGetWorkspaceKey().Returns(WorkspaceKey);
        UnitOfWork.Workspaces.GetBySlug(WorkspaceKey, isReadonly: true, cancellationToken: Arg.Any<CancellationToken>()).Returns(workspace);

        var context = CreateContext(user, requirement);
        await Handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse("pinning a task is a write even though it is gated on tasks.read");
    }

    [Fact]
    public async Task HandleRequirement_ShouldFail_ForAnonymousWhenThereIsNoRequestToInspect()
    {
        var user = AnonymousUser();
        var requirement = new WorkspacePermissionRequirement(NetptunePermissions.Tasks.Read);

        HttpContext.HttpContext.ReturnsNull();
        Identity.TryGetWorkspaceKey().Returns(WorkspaceKey);

        var context = CreateContext(user, requirement);
        await Handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
        await UnitOfWork.Workspaces.DidNotReceive().GetBySlug(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleRequirement_ShouldSucceed_ForAMemberWriteOnAMethodAnonymousCallersCannotUse()
    {
        var user = AuthenticatedUser();
        var requirement = new WorkspacePermissionRequirement(NetptunePermissions.Tasks.Read);

        RequestMethod(HttpMethods.Post);
        Identity.TryGetWorkspaceKey().Returns(WorkspaceKey);
        Identity.GetCurrentUserId().Returns(UserId);
        Cache.GetUserPermissions(UserId, WorkspaceKey).Returns(MakePermissions(WorkspaceRole.Member, NetptunePermissions.Tasks.Read));

        var context = CreateContext(user, requirement);
        await Handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue("the read-only rule applies to anonymous callers alone");
    }
}
